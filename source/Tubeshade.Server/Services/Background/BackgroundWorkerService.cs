using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration;
using Tubeshade.Server.Services.Migrations;
using static System.Data.IsolationLevel;

namespace Tubeshade.Server.Services.Background;

public sealed class BackgroundWorkerService : BackgroundService
{
    private const string TaskRunPrefix = "task-run";
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runCancellations = [];

    private readonly IServiceProvider _serviceProvider;
    private readonly SchedulerOptions _options;
    private readonly FileSystemService _fileSystemService;
    private readonly TaskListenerService _taskListenerService;

    private readonly SemaphoreSlim _indexLock;
    private readonly SemaphoreSlim _downloadLock;
    private readonly SemaphoreSlim _sponsorBlockLock;
    private readonly SemaphoreSlim _pubSubLock;

    public BackgroundWorkerService(
        IServiceProvider serviceProvider,
        IOptions<SchedulerOptions> options,
        FileSystemService fileSystemService,
        TaskListenerService taskListenerService)
    {
        _serviceProvider = serviceProvider;
        _fileSystemService = fileSystemService;
        _taskListenerService = taskListenerService;
        _options = options.Value;

        _indexLock = new(_options.IndexTaskLimit);
        _downloadLock = new(_options.DownloadTaskLimit);
        _sponsorBlockLock = new(_options.SponsorBlockTaskLimit);
        _pubSubLock = new(1);
    }

    internal async ValueTask<bool> CancelTaskRun(Guid taskRunId)
    {
        if (!_runCancellations.TryRemove(taskRunId, out var tokenSource))
        {
            return false;
        }

        try
        {
            await tokenSource.CancelAsync();
        }
        catch (Exception exception)
        {
            // Cancellation callbacks of the task run are invoked synchronously, and if one of them throws,
            // it should not stop the host. The token is still cancelled, so the task run is cancelled regardless.
            var logger = _serviceProvider.GetRequiredService<ILogger<BackgroundWorkerService>>();
            logger.CancellingTaskRunFailed(exception, taskRunId);
        }
        finally
        {
            tokenSource.Dispose();
        }

        return true;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Parallel.ForEachAsync(
            _taskListenerService.TaskRequested.ReadAllAsync(stoppingToken),
            new ParallelOptions
            {
                CancellationToken = stoppingToken,
                MaxDegreeOfParallelism = _options.WorkerCount,
            },
            ProcessTask);
    }

    private async ValueTask ProcessTask(Guid taskRequestId, CancellationToken cancellationToken)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<BackgroundWorkerService>>();
        using var requestScope = logger.BeginScope("{TaskRequestId}", taskRequestId);
        logger.ReceivedTaskRequest(taskRequestId);

        // Generated here instead of by the database, so that if committing the dequeue transaction fails
        // with an unknown outcome, the next attempt can tell whether the task run was added by this worker.
        var taskRunId = Guid.NewGuid();

        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = source.Token;
        if (!_runCancellations.TryAdd(taskRunId, source))
        {
            source.Dispose();
            throw new InvalidOperationException($"Could not add cancellation source for run {taskRunId}");
        }

        Guid taskId;
        TaskResult result;
        Exception? taskException;

        try
        {
            if (await DequeueTask(logger, taskRequestId, taskRunId, cancellationToken) is not var (task, taskSource))
            {
                return;
            }

            taskId = task.Id;

            await using var scope = _serviceProvider.CreateAsyncScope();
            using var loggerScope = logger.BeginScope("{TaskId}", taskId);

            var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
            var taskRepository = scope.ServiceProvider.GetRequiredService<TaskRepository>();
            var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();

            using var listener = new RunFinishedListener(_taskListenerService);

            try
            {
                await connection.OpenConnection(token);
                await taskService.WaitForBlockingTasks(listener.Reader, task, taskRunId, token);

                var factory = scope.ServiceProvider.GetRequiredService<CookiesServiceFactory>();

                // Separate scope is needed, so that all updates to task status are not within a transaction
                await using var taskScope = _serviceProvider.CreateAsyncScope();
                await Execute(task, taskSource, taskScope.ServiceProvider, taskRepository, taskRunId, factory, token);
                result = TaskResult.Successful;
                taskException = null;
            }
            catch (Exception exception) when (exception is TaskCanceledException or OperationCanceledException)
            {
                logger.TaskCancelled(exception);
                result = TaskResult.Cancelled;
                taskException = null;
            }
            catch (Exception exception)
            {
                logger.TaskFailed(exception);
                result = TaskResult.Failed;
                taskException = exception;
            }
        }
        finally
        {
            if (_runCancellations.TryRemove(taskRunId, out var tokenSource))
            {
                tokenSource.Dispose();
            }
        }

        await SaveResult(taskId, taskRunId, result, taskException, cancellationToken);
    }

    /// <summary>
    /// Deletes the task request and adds a task run for it, retrying transient errors until it succeeds or the service is stopped.
    /// The task request is only deleted together with adding the task run, so if this fails before committing,
    /// the request is queued again the next time the listener connects to the database.
    /// </summary>
    /// <returns>The task to run, or <see langword="null"/> if there is nothing to run.</returns>
    private async ValueTask<DequeueResult.DequeuedTask?> DequeueTask(
        ILogger logger,
        Guid taskRequestId,
        Guid taskRunId,
        CancellationToken stoppingToken)
    {
        try
        {
            var result = await RetryDelay.RetryAsync(
                async isRetry =>
                {
                    await using var scope = _serviceProvider.CreateAsyncScope();
                    var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
                    var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

                    // concurrent dequeues are prevented with row locks, and retries rely on ReadCommitted to see committed task runs
                    await using var transaction = await connection.OpenAndBeginTransaction(ReadCommitted, stoppingToken);

                    // The transaction of a previous attempt might still be committing and holding the lock on the request.
                    // Waiting for it to finish, instead of skipping the request, ensures that if it committed,
                    // its task run is visible to the following query, since each statement sees all committed changes at ReadCommitted.
                    var request = isRetry
                        ? await repository.TryDeleteTaskRequestWaitingForLock(taskRequestId, transaction)
                        : await repository.TryDeleteTaskRequest(taskRequestId, transaction);

                    if (request is null)
                    {
                        // Either another worker handled the same request, which happens if it was queued more than once,
                        // or a previous attempt of this worker committed without the confirmation being received.
                        if (await repository.FindTaskRunRequest(taskRunId, transaction) is not { } existingRun)
                        {
                            logger.TaskRequestAlreadyHandled(taskRequestId);
                            return DequeueResult.Nothing;
                        }

                        logger.ResumingTaskRun(taskRunId);
                        if (await repository.FindAsync(existingRun.TaskId, transaction) is { } existingTask)
                        {
                            return DequeueResult.Dequeued(existingTask, existingRun.Source);
                        }

                        return DequeueResult.Nothing;
                    }

                    // Locked in a separate statement, so that TryDequeueTask sees task runs committed by the previous holder of the lock.
                    // The transaction is rolled back, so that the request is not deleted and can be retried.
                    if (!await repository.TryLockTask(request.TaskId, transaction))
                    {
                        await transaction.RollbackAsync(stoppingToken);
                        return DequeueResult.Locked;
                    }

                    // the request is deleted even if the task is not dequeued, same as triggering a task that is already running
                    if (await repository.TryDequeueTask(request.TaskId, transaction) is not { } task)
                    {
                        logger.CouldNotDequeueTask(request.TaskId);
                        await transaction.CommitAsync(stoppingToken);
                        return DequeueResult.Nothing;
                    }

                    logger.AddingTaskRun(task.Id);
                    await repository.AddTaskRun(taskRunId, task.Id, request.Source, transaction);
                    await transaction.CommitAsync(stoppingToken);
                    return DequeueResult.Dequeued(task, request.Source);
                },
                static result => result.TaskLocked,
                (_, delay) => logger.DequeueTaskLocked(taskRequestId, delay),
                exception => RetryDelay.IsTransient(exception) && !stoppingToken.IsCancellationRequested,
                (exception, delay) => logger.DequeueTaskFailed(exception, taskRequestId, delay),
                stoppingToken);

            return result.Task;
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.DequeueTaskAborted(exception, taskRequestId);
            return null;
        }
    }

    private async ValueTask Execute(
        TaskEntity task,
        TaskSource source,
        IServiceProvider provider,
        TaskRepository taskRepository,
        Guid taskRunId,
        CookiesServiceFactory cookiesServiceFactory,
        CancellationToken cancellationToken)
    {
        if (task.Type == TaskType.Index)
        {
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);
            using var scopedDirectory = _fileSystemService.CreateTemporaryDirectory(TaskRunPrefix, taskRunId);

            var (libraryId, userId) = task.DestructureLibraryTask();
            var service = provider.GetRequiredService<YoutubeIndexingService>();
            var cookieService =
                cookiesServiceFactory.Create(userId, libraryId, scopedDirectory.Directory, cancellationToken);

            var result = await service.Index(
                task.Url!,
                libraryId,
                userId,
                task.ChannelId,
                scopedDirectory.Directory,
                source,
                cookieService,
                cancellationToken);

            task.ChannelId = result.ChannelId;
            task.PlaylistId = result.PlaylistId;
            task.VideoId = result.VideoId;
            await taskRepository.UpdateAsync(task);
        }
        else if (task.Type == TaskType.YouTubeFeedUpdate)
        {
            using var scopedDirectory = _fileSystemService.CreateTemporaryDirectory(TaskRunPrefix, taskRunId);

            var (libraryId, userId) = task.DestructureLibraryTask();
            var service = provider.GetRequiredService<YoutubeWebhookService>();
            var cookieService =
                cookiesServiceFactory.Create(userId, libraryId, scopedDirectory.Directory, cancellationToken);

            await service.ProcessWebhook(
                task.Payload!,
                libraryId,
                userId,
                task.ChannelId!.Value,
                cookieService,
                cancellationToken);
        }
        else if (task.Type == TaskType.DownloadVideo)
        {
            using var scope = await LockAsync(_downloadLock, taskRepository, taskRunId, cancellationToken);
            using var scopedDirectory = _fileSystemService.CreateTemporaryDirectory(TaskRunPrefix, taskRunId);

            var (libraryId, userId) = task.DestructureLibraryTask();
            var service = provider.GetRequiredService<YoutubeDownloadService>();
            var cookieService =
                cookiesServiceFactory.Create(userId, libraryId, scopedDirectory.Directory, cancellationToken);

            await service.DownloadVideo(
                libraryId,
                task.VideoId!.Value,
                userId,
                taskRepository,
                taskRunId,
                scopedDirectory.Directory,
                provider,
                cookieService,
                cancellationToken);
        }
        else if (task.Type == TaskType.ScanChannel)
        {
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);
            using var scopedDirectory = _fileSystemService.CreateTemporaryDirectory(TaskRunPrefix, taskRunId);

            var (libraryId, userId) = task.DestructureLibraryTask();
            var service = provider.GetRequiredService<YoutubeIndexingService>();
            var cookieService =
                cookiesServiceFactory.Create(userId, libraryId, scopedDirectory.Directory, cancellationToken);

            await service.ScanChannel(
                libraryId,
                task.ChannelId!.Value,
                task.AllVideos,
                userId,
                taskRepository,
                taskRunId,
                scopedDirectory.Directory,
                source,
                cookieService,
                cancellationToken);
        }
        else if (task.Type == TaskType.ScanPlaylist)
        {
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);
            using var scopedDirectory = _fileSystemService.CreateTemporaryDirectory(TaskRunPrefix, taskRunId);

            var (libraryId, userId) = task.DestructureLibraryTask();
            var service = provider.GetRequiredService<YoutubeIndexingService>();
            var cookieService =
                cookiesServiceFactory.Create(userId, libraryId, scopedDirectory.Directory, cancellationToken);

            await service.ScanPlaylist(
                libraryId,
                task.PlaylistId!.Value,
                task.AllVideos,
                userId,
                taskRepository,
                taskRunId,
                scopedDirectory.Directory,
                source,
                cookieService,
                cancellationToken);
        }
        else if (task.Type == TaskType.ScanSubscriptions)
        {
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);
            using var scopedDirectory = _fileSystemService.CreateTemporaryDirectory(TaskRunPrefix, taskRunId);

            var (libraryId, userId) = task.DestructureLibraryTask();
            var service = provider.GetRequiredService<YoutubeIndexingService>();
            var cookieService =
                cookiesServiceFactory.Create(userId, libraryId, scopedDirectory.Directory, cancellationToken);

            await service.ScanSubscriptions(
                libraryId,
                userId,
                taskRepository,
                taskRunId,
                scopedDirectory.Directory,
                source,
                cookieService,
                cancellationToken);
        }
        else if (task.Type == TaskType.ScanSponsorBlockSegments)
        {
            using var scope = await LockAsync(_sponsorBlockLock, taskRepository, taskRunId, cancellationToken);
            var service = provider.GetRequiredService<SponsorBlockService>();

            var (libraryId, userId) = task.DestructureLibraryTask();
            await service.ScanVideoSegments(libraryId, userId, taskRepository, taskRunId, cancellationToken);
        }
        else if (task.Type == TaskType.RefreshSubscriptions)
        {
            using var scope = await LockAsync(_pubSubLock, taskRepository, taskRunId, cancellationToken);
            var service = provider.GetRequiredService<SubscriptionsService>();
            await service.RefreshSubscriptions(cancellationToken);
        }
        else if (task.Type == TaskType.ReindexVideos)
        {
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);

            var (libraryId, userId) = task.DestructureLibraryTask();
            var service = provider.GetRequiredService<YoutubeIndexingService>();
            await service.Reindex(libraryId, userId, source, cancellationToken);
        }
        else if (task.Type == TaskType.UpdateSponsorBlockSegments)
        {
            using var scope = await LockAsync(_sponsorBlockLock, taskRepository, taskRunId, cancellationToken);

            var (libraryId, userId) = task.DestructureLibraryTask();
            var service = provider.GetRequiredService<SponsorBlockService>();
            await service.UpdateVideoSegments(libraryId, userId, taskRepository, taskRunId, cancellationToken);
        }
        else if (task.Type == TaskType.RefreshFileMetadata)
        {
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);

            var (libraryId, userId) = task.DestructureLibraryTask();
            var service = provider.GetRequiredService<FileMetadataService>();
            await service.AddMissingMetadata(libraryId, userId, taskRepository, taskRunId, cancellationToken);
        }
        else if (task.Type == TaskType.RefreshTrackFiles)
        {
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);

            var (libraryId, userId) = task.DestructureLibraryTask();
            var service = provider.GetRequiredService<TrackFileService>();
            await service.RefreshTrackFiles(libraryId, userId, taskRepository, taskRunId, cancellationToken);
        }
        else if (task.Type == TaskType.DeleteTasks)
        {
            await taskRepository.StartTaskRun(taskRunId, cancellationToken);
            var service = provider.GetRequiredService<TaskService>();
            await service.DeleteOldTasks(cancellationToken);
        }
    }

    private static async ValueTask<SemaphoreSlimExtensions.SemaphoreScope> LockAsync(
        SemaphoreSlim semaphore,
        TaskRepository repository,
        Guid taskRunId,
        CancellationToken cancellationToken)
    {
        var scope = await semaphore.LockAsync(cancellationToken);
        await repository.StartTaskRun(taskRunId, cancellationToken);

        return scope;
    }

    private async ValueTask SaveResult(
        Guid taskId,
        Guid taskRunId,
        TaskResult result,
        Exception? taskException,
        CancellationToken stoppingToken)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<BackgroundWorkerService>>();
        using var loggerScope = logger.BeginScope("{TaskId}", taskId);

        try
        {
            var resultId = await RetryDelay.RetryAsync(
                async _ =>
                {
                    // the first attempt is made even if the service is stopping, so that the result is not lost
                    await using var scope = _serviceProvider.CreateAsyncScope();
                    var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();
                    return await repository.FinishTask(taskRunId, result, taskException, CancellationToken.None);
                },
                exception => RetryDelay.IsTransient(exception) && !stoppingToken.IsCancellationRequested,
                (exception, delay) => logger.SavingTaskResultFailed(exception, taskRunId, delay),
                stoppingToken);

            if (resultId is null)
            {
                logger.TaskResultAlreadyExists(taskRunId, result.Name);
            }
        }
        catch (Exception exception)
        {
            logger.SavingTaskResultAborted(exception, taskRunId);
        }
    }
}
