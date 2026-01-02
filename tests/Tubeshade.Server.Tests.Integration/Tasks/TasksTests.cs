using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
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
            _libraryId = await CreateLibrary(_userId, scope.ServiceProvider, transaction);
            await transaction.CommitAsync();
        }
    }

    [Test]
    [Order(1)]
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

            (await repository.GetBlockingTaskRunIds(task1, CancellationToken.None)).Should().BeEmpty();

            task1RunId = await repository.AddTaskRun(taskId, TaskSource.User, transaction);
            await repository.StartTaskRun(task1RunId, CancellationToken.None);
            await transaction.CommitAsync();
        }

        TaskEntity task2;
        Guid task2RunId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var taskId = await repository.AddIndexTask("https://example.org", _libraryId, _userId, transaction);

            task2 = (await repository.TryDequeueTask(taskId, transaction))!;
            task2.Should().NotBeNull();
            task2RunId = await repository.AddTaskRun(taskId, TaskSource.User, transaction);
            await transaction.CommitAsync();
        }

        var blockingIds = await repository.GetBlockingTaskRunIds(task2, CancellationToken.None);
        blockingIds.Should().ContainSingle().Which.Should().Be(task1RunId);

        await using (var parallelScope = Fixture.Services.CreateAsyncScope())
        {
            var parallelRepository = parallelScope.ServiceProvider.GetRequiredService<TaskRepository>();
            await parallelRepository.CompleteTask(task1RunId, CancellationToken.None);
        }

        blockingIds = await repository.GetBlockingTaskRunIds(task2, CancellationToken.None);
        blockingIds.Should().BeEmpty();

        await repository.StartTaskRun(task2RunId, CancellationToken.None);
    }

    [Test]
    [Order(2)]
    public async Task CompleteStuckTasks()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        Guid taskId;
        Guid taskRunId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            taskId = await repository.AddIndexTask("https://example.org", _libraryId, _userId, transaction);
            taskRunId = await repository.AddTaskRun(taskId, TaskSource.User, transaction);
            await repository.StartTaskRun(taskRunId, CancellationToken.None);

            await transaction.CommitAsync();
        }

        var parameters = new TaskParameters { UserId = _userId, LibraryId = _libraryId, Limit = 100, Offset = 0, };

        var tasks = await repository.GetRunningTasks(parameters, CancellationToken.None);

        var runningTask = tasks.Should().ContainSingle(entity => entity.Id == taskId).Subject;
        using (new AssertionScope())
        {
            runningTask.RunId.Should().Be(taskRunId);
            runningTask.RunState.Should().Be(RunState.Running);
        }

        await repository.CompleteStuckTasks(CancellationToken.None);

        tasks = await repository.GetRunningTasks(parameters, CancellationToken.None);

        tasks.Should().AllSatisfy(entity => entity.RunState.Should().Be(RunState.Finished));
        runningTask = tasks.Should().ContainSingle(entity => entity.Id == taskId).Subject;
        using (new AssertionScope())
        {
            runningTask.RunId.Should().Be(taskRunId);
            runningTask.RunState.Should().Be(RunState.Finished);
            runningTask.Result.Should().Be(TaskResult.Failed);
        }
    }
}
