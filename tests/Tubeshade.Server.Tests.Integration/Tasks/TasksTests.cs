using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Tasks;

public sealed class TasksTests(ServerFixture fixture) : ServerTests(fixture)
{
    private Guid _userId;
    private Guid _libraryId;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
            _userId = await userRepository.GetSystemUserId(transaction);
            await transaction.CommitAsync();
        }

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var taskRepository = scope.ServiceProvider.GetRequiredService<TaskRepository>();
            var taskId = await taskRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Type = TaskType.RefreshSubscriptions,
                    UserId = _userId,
                },
                transaction);

            var scheduleRepository = scope.ServiceProvider.GetRequiredService<ScheduleRepository>();
            var scheduleId = await scheduleRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    TaskId = taskId!.Value,
                    CronExpression = "* * * * *",
                    TimeZoneId = "Etc/UTC",
                },
                transaction);

            var libraryRepository = scope.ServiceProvider.GetRequiredService<LibraryRepository>();
            _libraryId = (await libraryRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    StoragePath = string.Empty,
                    SubscriptionsScheduleId = scheduleId!.Value,
                },
                transaction))!.Value;

            await transaction.CommitAsync();
        }
    }

    [Test]
    public async Task IndexVideoWhileDownloading()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        var channelRepository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();
        var videoRepository = scope.ServiceProvider.GetRequiredService<VideoRepository>();
        Guid videoId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var channelId = await channelRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    StoragePath = string.Empty,
                    ExternalId = string.Empty,
                    ExternalUrl = string.Empty,
                    Availability = ExternalAvailability.Public,
                },
                transaction);

            videoId = (await videoRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    Description = string.Empty,
                    Categories = [],
                    Tags = [],
                    Type = VideoType.Video,
                    ChannelId = channelId!.Value,
                    StoragePath = string.Empty,
                    ExternalId = string.Empty,
                    ExternalUrl = "https://example.org",
                    PublishedAt = SystemClock.Instance.GetCurrentInstant(),
                    RefreshedAt = SystemClock.Instance.GetCurrentInstant(),
                    Availability = ExternalAvailability.Public,
                    Duration = Period.FromMinutes(10),
                },
                transaction))!.Value;

            await transaction.CommitAsync();
        }

        Guid task1RunId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var taskId = await repository.AddDownloadTask(videoId, _libraryId, _userId, transaction);

            var task1 = (await repository.TryDequeueTask(taskId, transaction))!;
            task1.Should().NotBeNull();

            var blockingIds = await repository.GetBlockingTaskRunIds(task1, transaction);
            blockingIds.Should().BeEmpty();

            task1RunId = await repository.AddTaskRun(taskId, transaction);
            await repository.StartTaskRun(task1RunId, transaction);
            await transaction.CommitAsync();
        }

        TaskEntity task2;
        Guid task2RunId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var taskId = await repository.AddIndexTask("https://example.org", _libraryId, _userId, transaction);

            task2 = (await repository.TryDequeueTask(taskId, transaction))!;
            task2.Should().NotBeNull();
            task2RunId = await repository.AddTaskRun(taskId, transaction);
            await transaction.CommitAsync();
        }

        await using (var transaction = await connection.OpenAndBeginTransaction(IsolationLevel.ReadCommitted))
        {
            var blockingIds = await repository.GetBlockingTaskRunIds(task2, transaction);
            blockingIds.Should().ContainSingle().Which.Should().Be(task1RunId);

            await using (var parallelScope = Fixture.Services.CreateAsyncScope())
            {
                var parallelRepository = parallelScope.ServiceProvider.GetRequiredService<TaskRepository>();
                await parallelRepository.CompleteTask(task1RunId, CancellationToken.None);
            }

            blockingIds = await repository.GetBlockingTaskRunIds(task2, transaction);
            blockingIds.Should().BeEmpty();

            await repository.StartTaskRun(task2RunId, transaction);
            await transaction.CommitAsync();
        }
    }
}
