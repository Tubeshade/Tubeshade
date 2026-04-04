using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
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
    }

    internal async ValueTask<bool> CancelTaskRun(Guid taskRunId)
    {
        if (!_runCancellations.TryRemove(taskRunId, out var tokenSource))
        {
            return false;
        }

        await tokenSource.CancelAsync();
        return true;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Parallel.ForEachAsync(
            _taskListenerService.TaskCreated.ReadAllAsync(stoppingToken),
            new ParallelOptions
            {
                CancellationToken = stoppingToken,
                MaxDegreeOfParallelism = _options.WorkerCount,
            },
            ProcessTask);
    }

    private async ValueTask ProcessTask(CreatedTask createdTask, CancellationToken cancellationToken)
    {
        var taskId = createdTask.Id;
        var taskSource = createdTask.Source;

        await using var scope = _serviceProvider.CreateAsyncScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BackgroundWorkerService>>();
        using var loggerScope = logger.BeginScope("{TaskId}", taskId);
        logger.ReceivedCreatedTask(taskId);

        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var taskRepository = scope.ServiceProvider.GetRequiredService<TaskRepository>();
        var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();

        TaskEntity? task;
        Guid taskRunId;

        using var listener = new RunFinishedListener(_taskListenerService);

        // we only do a single read at the start of the transaction, so ReadCommitted is ok
        await using (var dequeueTransaction = await connection.OpenAndBeginTransaction(IsolationLevel.ReadCommitted, cancellationToken))
        {
            task = await taskRepository.TryDequeueTask(taskId, dequeueTransaction);
            if (task is null)
            {
                logger.CouldNotDequeueTask(taskId);
                return;
            }

            logger.AddingTaskRun(taskId);
            taskRunId = await taskRepository.AddTaskRun(task.Id, taskSource, dequeueTransaction);
            await dequeueTransaction.CommitAsync(cancellationToken);
        }

        try
        {
            var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var added = _runCancellations.TryAdd(taskRunId, source);
            Trace.Assert(added);

            await taskService.WaitForBlockingTasks(listener.Reader, task, taskRunId, source.Token);

            var factory = scope.ServiceProvider.GetRequiredService<CookiesServiceFactory>();

            // Separate scope is needed, so that all updates to task status are not within a transaction
            await using var taskScope = _serviceProvider.CreateAsyncScope();
            await Execute(task, taskSource, taskScope.ServiceProvider, taskRepository, taskRunId, factory, source.Token);
            await taskRepository.CompleteTask(taskRunId, source.Token);
        }
        catch (Exception exception) when (exception is TaskCanceledException or OperationCanceledException)
        {
            logger.TaskCancelled(exception);
            await taskRepository.CancelledTask(taskRunId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.TaskFailed(exception);
            await taskRepository.FailedTask(taskRunId, exception, CancellationToken.None);
        }
        finally
        {
            if (_runCancellations.TryRemove(taskRunId, out var source))
            {
                source.Dispose();
            }
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
            var cookieService = cookiesServiceFactory.Create(userId, libraryId, scopedDirectory.Directory, cancellationToken);

            var result = await service.Index(
                task.Url!,
                libraryId,
                userId,
                task.VideoId,
                task.ChannelId,
                scopedDirectory.Directory,
                source,
                cookieService,
                cancellationToken);

            task.ChannelId = result.ChannelId;
            task.VideoId = result.VideoId;
            await taskRepository.UpdateAsync(task);
        }
        else if (task.Type == TaskType.DownloadVideo)
        {
            using var scope = await LockAsync(_downloadLock, taskRepository, taskRunId, cancellationToken);
            using var scopedDirectory = _fileSystemService.CreateTemporaryDirectory(TaskRunPrefix, taskRunId);

            var (libraryId, userId) = task.DestructureLibraryTask();
            var service = provider.GetRequiredService<YoutubeDownloadService>();
            var cookieService = cookiesServiceFactory.Create(userId, libraryId, scopedDirectory.Directory, cancellationToken);

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
            var cookieService = cookiesServiceFactory.Create(userId, libraryId, scopedDirectory.Directory, cancellationToken);

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
        else if (task.Type == TaskType.ScanSubscriptions)
        {
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);
            using var scopedDirectory = _fileSystemService.CreateTemporaryDirectory(TaskRunPrefix, taskRunId);

            var (libraryId, userId) = task.DestructureLibraryTask();
            var service = provider.GetRequiredService<YoutubeIndexingService>();
            var cookieService = cookiesServiceFactory.Create(userId, libraryId, scopedDirectory.Directory, cancellationToken);

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
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);
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
}
