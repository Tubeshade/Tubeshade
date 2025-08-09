using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;
using Tubeshade.Server.Configuration;

namespace Tubeshade.Server.Services.Background;

public sealed class BackgroundWorkerService : BackgroundService
{
    private static readonly SemaphoreSlim IndexLock = new(1);
    private static readonly SemaphoreSlim DownloadLock = new(1);
    private static readonly SemaphoreSlim SponsorBlockLock = new(1);

    private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> RunCancellations = [];

    private readonly IServiceProvider _serviceProvider;
    private readonly SchedulerOptions _options;

    public BackgroundWorkerService(IServiceProvider serviceProvider, IOptions<SchedulerOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
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

    private static async ValueTask Execute(
        TaskEntity task,
        IServiceProvider provider,
        DirectoryInfo tempDirectory,
        TaskRepository taskRepository,
        Guid taskRunId,
        CancellationToken cancellationToken)
    {
        if (task.Type == TaskType.Index)
        {
            using var scope = await IndexLock.LockAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize(task.Payload, TaskPayloadContext.Default.IndexPayload)!;
            var service = provider.GetRequiredService<YoutubeService>();
            await service.Index(
                payload.Url,
                payload.LibraryId,
                payload.UserId,
                tempDirectory,
                cancellationToken);
        }
        else if (task.Type == TaskType.DownloadVideo)
        {
            using var scope = await DownloadLock.LockAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize(task.Payload, TaskPayloadContext.Default.DownloadVideoPayload)!;
            var service = provider.GetRequiredService<YoutubeService>();
            await service.DownloadVideo(
                payload.LibraryId,
                payload.VideoId,
                payload.UserId,
                taskRepository,
                taskRunId,
                tempDirectory,
                cancellationToken);
        }
        else if (task.Type == TaskType.ScanChannel)
        {
            using var scope = await IndexLock.LockAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize(task.Payload, TaskPayloadContext.Default.ScanChannelPayload)!;
            var service = provider.GetRequiredService<YoutubeService>();
            await service.ScanChannel(
                payload.LibraryId,
                payload.ChannelId,
                payload.All,
                payload.UserId,
                taskRepository,
                taskRunId,
                tempDirectory,
                cancellationToken);
        }
        else if (task.Type == TaskType.ScanSubscriptions)
        {
            using var scope = await IndexLock.LockAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize(task.Payload, TaskPayloadContext.Default.ScanSubscriptionsPayload)!;
            var service = provider.GetRequiredService<YoutubeService>();
            await service.ScanSubscriptions(
                payload.LibraryId,
                payload.UserId,
                taskRepository,
                taskRunId,
                tempDirectory,
                cancellationToken);
        }
        else if (task.Type == TaskType.ScanSponsorBlockSegments)
        {
            using var scope = await SponsorBlockLock.LockAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize(task.Payload, TaskPayloadContext.Default.ScanSponsorBlockSegmentsPayload)!;
            var service = provider.GetRequiredService<YoutubeService>();
            await service.ScanSponsorBlockSegments(
                payload.LibraryId,
                payload.UserId,
                taskRepository,
                taskRunId,
                tempDirectory,
                cancellationToken);
        }
    }
}
