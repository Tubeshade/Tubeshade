using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration;

namespace Tubeshade.Server.Services;

public abstract class ChannelConsumerBackgroundService<TService, TPayload> : BackgroundService
    where TService : notnull
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ChannelReader<Guid> _channelReader;
    private readonly TaskType _taskType;

    protected ChannelConsumerBackgroundService(
        IServiceProvider serviceProvider,
        ChannelReader<Guid> channelReader,
        TaskType taskType)
    {
        _serviceProvider = serviceProvider;
        _channelReader = channelReader;
        _taskType = taskType;
    }

    protected virtual int Parallelism => 1;

    protected abstract JsonTypeInfo<TPayload> PayloadTypeInfo { get; }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Parallel.ForEachAsync(
            _channelReader.ReadAllAsync(stoppingToken),
            new ParallelOptions
            {
                CancellationToken = stoppingToken,
                MaxDegreeOfParallelism = Parallelism,
            },
            ProcessTask);
    }

    protected abstract ValueTask ProcessTaskPayload(
        TaskContext<TService, TPayload> context,
        CancellationToken cancellationToken);

    private async ValueTask ProcessTask(Guid taskId, CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<YtdlpOptions>>().Value;

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DownloadBackgroundService>>();
        using var loggerScope = logger.BeginScope("{TaskId}", taskId);
        logger.LogDebug("Received created task id");

        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var taskRepository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        await using var transaction = await connection.OpenAndBeginTransaction(cancellationToken);

        var task = await taskRepository.TryDequeueTask(taskId, _taskType, transaction);
        if (task is null)
        {
            logger.LogDebug("Could not dequeue task {TaskId} of type {TaskType}", taskId, _taskType.Name);
            return;
        }

        var taskRunId = await taskRepository.StartTask(task.Id, transaction);
        var taskRunDirectoryPath = Path.Combine(options.TempPath, $"task-run_{taskRunId:N}");
        await transaction.CommitAsync(cancellationToken);

        try
        {
            var taskRunDirectory = Directory.CreateDirectory(taskRunDirectoryPath);

            // Separate scope is needed, so that all updates to task status are not withing a transaction
            await using var internalScope = _serviceProvider.CreateAsyncScope();

            var payload = JsonSerializer.Deserialize(task.Payload, PayloadTypeInfo)!;
            var service = internalScope.ServiceProvider.GetRequiredService<TService>();

            await ProcessTaskPayload(
                new TaskContext<TService, TPayload>
                {
                    Service = service,
                    Payload = payload,
                    TaskRepository = taskRepository,
                    TaskRunId = taskRunId,
                    Directory = taskRunDirectory,
                },
                cancellationToken);

            await taskRepository.CompleteTask(taskRunId, cancellationToken);
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
        }
    }
}
