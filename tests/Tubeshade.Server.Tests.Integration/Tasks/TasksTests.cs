using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Videos;
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
            task1RunId = Guid.NewGuid();
            await repository.AddTaskRun(task1RunId, taskId, TaskSource.User, transaction);

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
                indexTaskRunId = Guid.NewGuid();
                await repository.AddTaskRun(indexTaskRunId, indexTaskId, TaskSource.User, transaction);
                await transaction.CommitAsync();
            }

            var parameters = BlockingTaskParameters.FromTask(indexTask, indexTaskRunId);

            var blockingIds = await repository.GetBlockingTaskRunIds(parameters, CancellationToken.None);
            blockingIds.Should().ContainSingle().Which.Should().Be(task1RunId);

            await using (var parallelScope = Fixture.Services.CreateAsyncScope())
            {
                var parallelRepository = parallelScope.ServiceProvider.GetRequiredService<TaskRepository>();
                await parallelRepository.FinishTask(task1RunId, TaskResult.Successful, null, CancellationToken.None);
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
            taskRunId = Guid.NewGuid();
            await repository.AddTaskRun(taskRunId, taskId, TaskSource.User, transaction);
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
            taskRunId = Guid.NewGuid();
            await repository.AddTaskRun(taskRunId, taskId, TaskSource.User, transaction);

            var otherTaskId = await repository.AddTask(TaskEntity.Index(_libraryId, _userId, url2), transaction);
            var otherTaskRunId = Guid.NewGuid();
            await repository.AddTaskRun(otherTaskRunId, otherTaskId, TaskSource.User, transaction);
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

        await repository.FinishTask(taskRunId, TaskResult.Successful, null, CancellationToken.None);

        var ids = await Task.WhenAll(AddTask(url1), AddTask(url1), AddTask(url1));
        ids.Should().ContainSingle(id => id.HasValue);
    }

    [Test]
    public async Task FinishTaskTwice()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        Guid taskRunId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var url = $"https://example.org/{Guid.NewGuid():N}";
            var taskId = await repository.AddTask(TaskEntity.Index(_libraryId, _userId, url), transaction);
            taskRunId = Guid.NewGuid();
            await repository.AddTaskRun(taskRunId, taskId, TaskSource.User, transaction);
            await transaction.CommitAsync();
        }

        var resultId = await repository.FinishTask(taskRunId, TaskResult.Successful, null, CancellationToken.None);
        var secondResultId =
            await repository.FinishTask(taskRunId, TaskResult.Failed, new Exception("Retry"), CancellationToken.None);

        using (new AssertionScope())
        {
            resultId.Should().NotBeNull();
            secondResultId.Should().BeNull();
        }
    }

    [Test]
    public async Task TaskRequestDequeuedOnce()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        var url = $"https://example.org/{Guid.NewGuid():N}";
        Guid taskId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            taskId = await repository.AddTask(TaskEntity.Index(_libraryId, _userId, url), transaction);
            await transaction.CommitAsync();
        }

        // the notification is only sent on commit, so the background worker does not handle the request
        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            await repository.TriggerTask(taskId, TaskSource.User, _userId, transaction);
            var requestId = await connection.QuerySingleAsync<Guid>(
                "SELECT id FROM tasks.task_requests WHERE task_id = @taskId;",
                new { taskId },
                transaction);

            var request = await repository.TryDeleteTaskRequest(requestId, transaction);
            var secondRequest = await repository.TryDeleteTaskRequest(requestId, transaction);

            using (new AssertionScope())
            {
                request.Should().BeEquivalentTo(new TaskRequest { TaskId = taskId, Source = TaskSource.User });
                secondRequest.Should().BeNull();
            }

            var taskRunId = Guid.NewGuid();
            await repository.AddTaskRun(taskRunId, taskId, TaskSource.User, transaction);

            var runRequest = await repository.FindTaskRunRequest(taskRunId, transaction);
            runRequest.Should().BeEquivalentTo(new TaskRequest { TaskId = taskId, Source = TaskSource.User });

            await transaction.RollbackAsync();
        }
    }

    [Test]
    public async Task TryLockTask()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        await using var otherScope = Fixture.Services.CreateAsyncScope();
        var otherConnection = otherScope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var otherRepository = otherScope.ServiceProvider.GetRequiredService<TaskRepository>();

        var url = $"https://example.org/{Guid.NewGuid():N}";
        Guid taskId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            taskId = await repository.AddTask(TaskEntity.Index(_libraryId, _userId, url), transaction);
            await transaction.CommitAsync();
        }

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var locked = await repository.TryLockTask(taskId, transaction);

            await using var otherTransaction = await otherConnection.OpenAndBeginTransaction();
            var otherLocked = await otherRepository.TryLockTask(taskId, otherTransaction);

            using (new AssertionScope())
            {
                locked.Should().BeTrue();
                otherLocked.Should().BeFalse();
            }

            await otherTransaction.RollbackAsync();
            await transaction.RollbackAsync();
        }

        await using (var transaction = await otherConnection.OpenAndBeginTransaction())
        {
            var locked = await otherRepository.TryLockTask(taskId, transaction);
            locked.Should().BeTrue("the lock is released when the other transaction ends");
            await transaction.RollbackAsync();
        }
    }

    [Test]
    public async Task DequeueDuringConcurrentTrigger()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        await using var otherScope = Fixture.Services.CreateAsyncScope();
        var otherConnection = otherScope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var otherRepository = otherScope.ServiceProvider.GetRequiredService<TaskRepository>();

        var url = $"https://example.org/{Guid.NewGuid():N}";
        Guid taskId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            taskId = await repository.AddTask(TaskEntity.Index(_libraryId, _userId, url), transaction);
            await transaction.CommitAsync();
        }

        // the foreign key check of the new request takes a FOR KEY SHARE lock on the task
        await using var triggerTransaction = await otherConnection.OpenAndBeginTransaction();
        await otherRepository.TriggerTask(taskId, TaskSource.User, triggerTransaction);

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var locked = await repository.TryLockTask(taskId, transaction);
            var dequeued = await repository.TryDequeueTask(taskId, transaction);

            using (new AssertionScope())
            {
                locked.Should().BeTrue();
                dequeued.Should().NotBeNull();
            }

            await transaction.RollbackAsync();
        }

        await triggerTransaction.RollbackAsync();
    }

    [Test]
    public async Task RetryWaitsForPreviousAttempt()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        await using var retryScope = Fixture.Services.CreateAsyncScope();
        var retryConnection = retryScope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var retryRepository = retryScope.ServiceProvider.GetRequiredService<TaskRepository>();

        var url = $"https://example.org/{Guid.NewGuid():N}";
        Guid taskId;
        Guid requestId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            taskId = await repository.AddTask(TaskEntity.Index(_libraryId, _userId, url), transaction);
            await repository.TriggerTask(taskId, TaskSource.User, transaction);
            requestId = await connection.QuerySingleAsync<Guid>(
                "SELECT id FROM tasks.task_requests WHERE task_id = @taskId;",
                new { taskId },
                transaction);

            await transaction.CommitAsync();
        }

        var taskRunId = Guid.NewGuid();

        // the first attempt deletes the request and adds the task run, but has not committed yet
        await using var firstAttempt = await connection.OpenAndBeginTransaction(IsolationLevel.ReadCommitted);
        (await repository.TryDeleteTaskRequest(requestId, firstAttempt)).Should().NotBeNull();
        await repository.AddTaskRun(taskRunId, taskId, TaskSource.User, firstAttempt);

        await using var retry = await retryConnection.OpenAndBeginTransaction(IsolationLevel.ReadCommitted);
        (await retryRepository.TryDeleteTaskRequest(requestId, retry))
            .Should().BeNull("the request is skipped while it is locked");

        var waitingDelete = retryRepository.TryDeleteTaskRequestWaitingForLock(requestId, retry).AsTask();
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        waitingDelete.IsCompleted.Should().BeFalse("the retry waits for the first attempt to finish");

        await firstAttempt.CommitAsync();

        using (new AssertionScope())
        {
            (await waitingDelete).Should().BeNull("the first attempt already deleted the request");
            var runRequest = await retryRepository.FindTaskRunRequest(taskRunId, retry);
            runRequest.Should().BeEquivalentTo(new TaskRequest { TaskId = taskId, Source = TaskSource.User });
        }

        await retry.RollbackAsync();
    }

    [Test]
    public async Task TriggerMissingTask()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        var taskId = Guid.NewGuid();

        await using var transaction = await connection.OpenAndBeginTransaction();
        await repository.TriggerTask(taskId, TaskSource.Schedule, transaction);

        var count = await connection.QuerySingleAsync<int>(
            "SELECT count(*) FROM tasks.task_requests WHERE task_id = @taskId;",
            new { taskId },
            transaction);

        count.Should().Be(0);
        await transaction.RollbackAsync();
    }

    [Test]
    public async Task FeedUpdates()
    {
        var token = CancellationToken.None;

        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        Guid channelId1;
        Guid channelId2;
        Guid videoId;

        await using (var transaction = await connection.OpenAndBeginTransaction(cancellationToken: token))
        {
            var channelRepository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();
            var videoRepository = scope.ServiceProvider.GetRequiredService<VideoRepository>();
            channelId1 = await CreateChannel(channelRepository, transaction);
            channelId2 = await CreateChannel(channelRepository, transaction);
            videoId = await CreateVideo(channelId1, Guid.NewGuid().ToString("N"), videoRepository, transaction);

            await transaction.CommitAsync(token);
        }

        Guid updateRunId;

        await using (var transaction = await connection.OpenAndBeginTransaction(cancellationToken: token))
        {
            var task = TaskEntity.YouTubeFeedUpdate(_libraryId, _userId, channelId1, string.Empty);
            var taskId = await repository.AddTask(task, transaction);
            updateRunId = Guid.NewGuid();
            await repository.AddTaskRun(updateRunId, taskId, TaskSource.Webhook, transaction);
            await repository.StartTaskRun(updateRunId, token);
            await transaction.CommitAsync(token);
        }

        TaskEntity blockedTask;
        Guid blockedRunId;

        TaskEntity unblockedTask;
        Guid unblockedRunId;

        TaskEntity indexTask;
        Guid indexTaskRunId;

        await using (var transaction = await connection.OpenAndBeginTransaction(token))
        {
            blockedTask = TaskEntity.YouTubeFeedUpdate(_libraryId, _userId, channelId1, string.Empty);
            var blockedTaskId = await repository.AddTask(blockedTask, transaction);
            blockedRunId = Guid.NewGuid();
            await repository.AddTaskRun(blockedRunId, blockedTaskId, TaskSource.Webhook, transaction);

            unblockedTask = TaskEntity.YouTubeFeedUpdate(_libraryId, _userId, channelId2, string.Empty);
            var unblockedTaskId = await repository.AddTask(unblockedTask, transaction);
            unblockedRunId = Guid.NewGuid();
            await repository.AddTaskRun(unblockedRunId, unblockedTaskId, TaskSource.Webhook, transaction);

            indexTask = TaskEntity.Index(_libraryId, _userId, channelId1, videoId, Guid.NewGuid().ToString("N"));
            var indexTaskId = await repository.AddTask(indexTask, transaction);
            indexTaskRunId = Guid.NewGuid();
            await repository.AddTaskRun(indexTaskRunId, indexTaskId, TaskSource.Webhook, transaction);

            await transaction.CommitAsync(token);
        }

        using var assertionScope = new AssertionScope();

        (await repository.GetBlockingTaskRunIds(BlockingTaskParameters.FromTask(blockedTask, blockedRunId), token))
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(updateRunId);

        (await repository.GetBlockingTaskRunIds(BlockingTaskParameters.FromTask(indexTask, indexTaskRunId), token))
            .Should()
            .BeEmpty();

        (await repository.GetBlockingTaskRunIds(BlockingTaskParameters.FromTask(unblockedTask, unblockedRunId), token))
            .Should()
            .BeEmpty();
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
                    await repository.AddTaskRun(Guid.NewGuid(), taskId.Value, TaskSource.User, transaction);
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