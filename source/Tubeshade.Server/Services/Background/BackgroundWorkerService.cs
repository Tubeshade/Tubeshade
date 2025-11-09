using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
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

    private async ValueTask ProcessTask(Guid taskId, CancellationToken cancellationToken)
    {
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

        await using (var dequeueTransaction = await connection.OpenAndBeginTransaction(cancellationToken))
        {
            task = await taskRepository.TryDequeueTask(taskId, dequeueTransaction);
            if (task is null)
            {
                logger.CouldNotDequeueTask(taskId);
                return;
            }

            logger.AddingTaskRun(taskId);
            taskRunId = await taskRepository.AddTaskRun(task.Id, dequeueTransaction);
            await dequeueTransaction.CommitAsync(cancellationToken);
        }

        try
        {
            var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var added = _runCancellations.TryAdd(taskRunId, source);
            Trace.Assert(added);

            await taskService.WaitForBlockingTasks(listener.Reader, task, taskRunId, source.Token);

            using var scopedDirectory = _fileSystemService.CreateTemporaryDirectory("task-run", taskRunId);

            // Separate scope is needed, so that all updates to task status are not within a transaction
            await using var taskScope = _serviceProvider.CreateAsyncScope();
            await Execute(task, taskScope.ServiceProvider, scopedDirectory.Directory, taskRepository, taskRunId, source.Token);
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
            _ = _runCancellations.TryRemove(taskRunId, out _);
        }
    }

    private async ValueTask Execute(
        TaskEntity task,
        IServiceProvider provider,
        DirectoryInfo tempDirectory,
        TaskRepository taskRepository,
        Guid taskRunId,
        CancellationToken cancellationToken)
    {
        if (task.Type == TaskType.Index)
        {
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);
            var service = provider.GetRequiredService<YoutubeService>();
            var result = await service.Index(
                task.Url!,
                task.LibraryId!.Value,
                task.UserId!.Value,
                tempDirectory,
                cancellationToken);

            task.ChannelId = result.ChannelId;
            task.VideoId = result.VideoId;
            await taskRepository.UpdateAsync(task);
        }
        else if (task.Type == TaskType.DownloadVideo)
        {
            using var scope = await LockAsync(_downloadLock, taskRepository, taskRunId, cancellationToken);
            var service = provider.GetRequiredService<YoutubeService>();
            await service.DownloadVideo(
                task.LibraryId!.Value,
                task.VideoId!.Value,
                task.UserId!.Value,
                taskRepository,
                taskRunId,
                tempDirectory,
                cancellationToken);
        }
        else if (task.Type == TaskType.ScanChannel)
        {
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);
            var service = provider.GetRequiredService<YoutubeService>();
            await service.ScanChannel(
                task.LibraryId!.Value,
                task.ChannelId!.Value,
                task.AllVideos,
                task.UserId!.Value,
                taskRepository,
                taskRunId,
                tempDirectory,
                cancellationToken);
        }
        else if (task.Type == TaskType.ScanSubscriptions)
        {
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);
            var service = provider.GetRequiredService<YoutubeService>();
            await service.ScanSubscriptions(
                task.LibraryId!.Value,
                task.UserId!.Value,
                taskRepository,
                taskRunId,
                tempDirectory,
                cancellationToken);
        }
        else if (task.Type == TaskType.ScanSponsorBlockSegments)
        {
            using var scope = await LockAsync(_sponsorBlockLock, taskRepository, taskRunId, cancellationToken);
            var service = provider.GetRequiredService<YoutubeService>();
            await service.ScanSponsorBlockSegments(
                task.LibraryId!.Value,
                task.UserId!.Value,
                taskRepository,
                taskRunId,
                tempDirectory,
                cancellationToken);
        }
        else if (task.Type == TaskType.RefreshSubscriptions)
        {
            using var scope = await LockAsync(_indexLock, taskRepository, taskRunId, cancellationToken);
            var service = provider.GetRequiredService<SubscriptionsService>();
            await service.RefreshSubscriptions(cancellationToken);
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
