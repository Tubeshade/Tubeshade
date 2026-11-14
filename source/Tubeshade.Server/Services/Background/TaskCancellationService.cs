using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Services.Background;

public sealed class TaskCancellationService : BackgroundService
{
    private readonly ILogger<TaskCancellationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TaskListenerService _taskListenerService;
    private readonly BackgroundWorkerService _backgroundWorkerService;

    public TaskCancellationService(
        ILogger<TaskCancellationService> logger,
        IServiceProvider serviceProvider,
        TaskListenerService taskListenerService,
        BackgroundWorkerService backgroundWorkerService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _taskListenerService = taskListenerService;
        _backgroundWorkerService = backgroundWorkerService;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _taskListenerService.TaskRunCancelled;

        while (await reader.WaitToReadAsync(stoppingToken))
        {
            var taskRunId = await reader.ReadAsync(stoppingToken);
            if (await _backgroundWorkerService.CancelTaskRun(taskRunId))
            {
                continue;
            }

            await LogNoTaskRunToCancel(taskRunId, stoppingToken);
        }
    }

    private async ValueTask LogNoTaskRunToCancel(long taskRunId, CancellationToken stoppingToken)
    {
        try
        {
            // The run is not running here, so only the database knows its public id
            await using var scope = _serviceProvider.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();
            var publicId = await repository.FindTaskRunPublicIdForSystemAsync(SystemPrincipals.TaskRuns, taskRunId, stoppingToken);
            _logger.NoTaskRunToCancel(publicId);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            // Only for logging, so a failure, for example after the database restarted, must not stop listening for cancellations
            _logger.FindingTaskRunToCancelFailed(exception);
        }
    }
}
