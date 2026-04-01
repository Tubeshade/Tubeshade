using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
    public async Task IndexVideoWhileDownloading()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        var channelRepository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();
        var videoRepository = scope.ServiceProvider.GetRequiredService<VideoRepository>();

        var url = $"https://example.org/{Guid.NewGuid():N}";
        Guid videoId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var channelId = await CreateChannel(channelRepository, transaction);
            videoId = await CreateVideo(channelId, url, videoRepository, transaction);

            await transaction.CommitAsync();
        }

        Guid indexTaskId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            indexTaskId = await repository.AddTask(TaskEntity.Index(_libraryId, _userId, url), transaction);
            await transaction.CommitAsync();
        }

        Guid task1RunId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var taskId = await repository.AddTask(TaskEntity.Download(_libraryId, _userId, videoId), transaction);

            var task1 = (await repository.TryDequeueTask(taskId, transaction))!;
            task1.Should().NotBeNull();
            task1RunId = await repository.AddTaskRun(taskId, TaskSource.User, transaction);

            var parameters = BlockingTaskParameters.FromTask(task1, task1RunId);
            (await repository.GetBlockingTaskRunIds(parameters, CancellationToken.None)).Should().BeEmpty();

            await repository.StartTaskRun(task1RunId, CancellationToken.None);
            await transaction.CommitAsync();
        }

        {
            TaskEntity indexTask;
            Guid indexTaskRunId;

            await using (var transaction = await connection.OpenAndBeginTransaction())
            {
                indexTask = (await repository.TryDequeueTask(indexTaskId, transaction))!;
                indexTask.Should().NotBeNull();
                indexTaskRunId = await repository.AddTaskRun(indexTaskId, TaskSource.User, transaction);
                await transaction.CommitAsync();
            }

            var parameters = BlockingTaskParameters.FromTask(indexTask, indexTaskRunId);

            var blockingIds = await repository.GetBlockingTaskRunIds(parameters, CancellationToken.None);
            blockingIds.Should().ContainSingle().Which.Should().Be(task1RunId);

            await using (var parallelScope = Fixture.Services.CreateAsyncScope())
            {
                var parallelRepository = parallelScope.ServiceProvider.GetRequiredService<TaskRepository>();
                await parallelRepository.CompleteTask(task1RunId, CancellationToken.None);
            }

            blockingIds = await repository.GetBlockingTaskRunIds(parameters, CancellationToken.None);
            blockingIds.Should().BeEmpty();

            await repository.StartTaskRun(indexTaskRunId, CancellationToken.None);
        }
    }

    [Test]
    public async Task CompleteStuckTasks()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        var url = $"https://example.org/{Guid.NewGuid():N}";
        Guid taskId;
        Guid taskRunId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            taskId = await repository.AddTask(TaskEntity.Index(_libraryId, _userId, url), transaction);
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

    [Test]
    public async Task DuplicateTasks()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        var url1 = $"https://example.org/{Guid.NewGuid():N}";
        var url2 = $"https://example.org/{Guid.NewGuid():N}";
        Guid taskRunId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var taskId = await repository.AddTask(TaskEntity.Index(_libraryId, _userId, url1), transaction);
            taskRunId = await repository.AddTaskRun(taskId, TaskSource.User, transaction);

            var otherTaskId = await repository.AddTask(TaskEntity.Index(_libraryId, _userId, url2), transaction);
            var otherTaskRunId = await repository.AddTaskRun(otherTaskId, TaskSource.User, transaction);
            await repository.StartTaskRun(otherTaskRunId, CancellationToken.None);

            await transaction.CommitAsync();
        }

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var secondTaskId = await repository.TryAddTask(TaskEntity.Index(_libraryId, _userId, url1), transaction);
            secondTaskId.Should().BeNull();
            await transaction.CommitAsync();
        }

        await repository.StartTaskRun(taskRunId, CancellationToken.None);

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var secondTaskId = await repository.TryAddTask(TaskEntity.Index(_libraryId, _userId, url1), transaction);
            secondTaskId.Should().BeNull();
            await transaction.CommitAsync();
        }

        await repository.CompleteTask(taskRunId, CancellationToken.None);

        var ids = await Task.WhenAll(AddTask(url1), AddTask(url1), AddTask(url1));
        ids.Should().ContainSingle(id => id.HasValue);
    }

    private async Task<Guid?> AddTask(string url)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        return await connection.ExecuteWithinTransaction(
            NullLogger.Instance,
            async transaction =>
            {
                var taskId = await repository.TryAddTask(TaskEntity.Index(_libraryId, _userId, url), transaction);
                if (taskId.HasValue)
                {
                    await repository.AddTaskRun(taskId.Value, TaskSource.User, transaction);
                }

                return taskId;
            });
    }

    private async Task<Guid> CreateChannel(ChannelRepository repository, NpgsqlTransaction transaction)
    {
        var id = await repository.AddAsync(
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

        return id.Should().HaveValue().And.Subject!.Value;
    }

    private async Task<Guid> CreateVideo(
        Guid channelId,
        string url,
        VideoRepository repository,
        NpgsqlTransaction transaction)
    {
        var id = await repository.AddAsync(
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
                ChannelId = channelId,
                StoragePath = string.Empty,
                ExternalId = string.Empty,
                ExternalUrl = url,
                PublishedAt = SystemClock.Instance.GetCurrentInstant(),
                RefreshedAt = SystemClock.Instance.GetCurrentInstant(),
                Availability = ExternalAvailability.Public,
                Duration = Period.FromMinutes(10),
            },
            transaction);

        return id.Should().HaveValue().And.Subject!.Value;
    }
}
