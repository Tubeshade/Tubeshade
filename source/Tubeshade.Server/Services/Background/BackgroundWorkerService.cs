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
    private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> RunCancellations = [];

    private readonly IServiceProvider _serviceProvider;
    private readonly SchedulerOptions _options;

    private readonly SemaphoreSlim _indexLock;
    private readonly SemaphoreSlim _downloadLock;
    private readonly SemaphoreSlim _sponsorBlockLock;

    public BackgroundWorkerService(IServiceProvider serviceProvider, IOptions<SchedulerOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;

        _indexLock = new(_options.IndexTaskLimit);
        _downloadLock = new(_options.DownloadTaskLimit);
        _sponsorBlockLock = new(_options.SponsorBlockTaskLimit);
    }

    internal static async ValueTask<bool> CancelTaskRun(Guid taskRunId)
    {
        if (!RunCancellations.TryRemove(taskRunId, out var tokenSource))
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
            TaskListenerService.TaskCreated.ReadAllAsync(stoppingToken),
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
        var options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<YtdlpOptions>>().Value;

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BackgroundWorkerService>>();
        using var loggerScope = logger.BeginScope("{TaskId}", taskId);
        logger.LogDebug("Received created task id");

        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var taskRepository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        TaskEntity? task;
        Guid taskRunId;

        await using (var dequeueTransaction = await connection.OpenAndBeginTransaction(cancellationToken))
        {
            task = await taskRepository.TryDequeueTask(taskId, dequeueTransaction);
            if (task is null)
            {
                logger.LogDebug("Could not dequeue task {TaskId}", taskId);
                return;
            }

            taskRunId = await taskRepository.AddTaskRun(task.Id, dequeueTransaction);
            await dequeueTransaction.CommitAsync(cancellationToken);
        }

        var taskRunDirectoryPath = Path.Combine(options.TempPath, $"task-run_{taskRunId:N}");

        try
        {
            var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var added = RunCancellations.TryAdd(taskRunId, source);
            Trace.Assert(added);

            var taskRunDirectory = Directory.CreateDirectory(taskRunDirectoryPath);

            // Separate scope is needed, so that all updates to task status are not within a transaction
            await using var taskScope = _serviceProvider.CreateAsyncScope();
            await Execute(task, taskScope.ServiceProvider, taskRunDirectory, taskRepository, taskRunId, source.Token);
            await taskRepository.CompleteTask(taskRunId, source.Token);
        }
        catch (TaskCanceledException exception)
        {
            logger.LogWarning(exception, "Task cancelled");
            await taskRepository.CancelledTask(taskRunId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Task failed unexpectedly");
            await taskRepository.FailedTask(taskRunId, exception, CancellationToken.None);
        }
        finally
        {
            if (Directory.Exists(taskRunDirectoryPath))
            {
                logger.LogDebug("Deleting temporary task run directory {Path}", taskRunDirectoryPath);
                Directory.Delete(taskRunDirectoryPath, true);
            }
            else
            {
                logger.LogDebug("Temporary task run directory does not exist at {Path}", taskRunDirectoryPath);
            }

            _ = RunCancellations.TryRemove(taskRunId, out _);
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
            using var scope = await _indexLock.LockAsync(cancellationToken);
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
            using var scope = await _downloadLock.LockAsync(cancellationToken);
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
            using var scope = await _indexLock.LockAsync(cancellationToken);
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
            using var scope = await _indexLock.LockAsync(cancellationToken);
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
            using var scope = await _sponsorBlockLock.LockAsync(cancellationToken);
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
            using var scope = await _indexLock.LockAsync(cancellationToken);
            var service = provider.GetRequiredService<SubscriptionsService>();
            await service.RefreshSubscriptions(cancellationToken);
        }
    }
}
