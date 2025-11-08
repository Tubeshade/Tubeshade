using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tubeshade.Server.Services.Background;

public sealed class TaskCancellationService : BackgroundService
{
    private readonly ILogger<TaskCancellationService> _logger;
    private readonly TaskListenerService _taskListenerService;
    private readonly BackgroundWorkerService _backgroundWorkerService;

    public TaskCancellationService(
        ILogger<TaskCancellationService> logger,
        TaskListenerService taskListenerService,
        BackgroundWorkerService backgroundWorkerService)
    {
        _logger = logger;
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
            _logger.LogDebug("Cancelling task run {TaskRunId}", taskRunId);

            var cancelled = await _backgroundWorkerService.CancelTaskRun(taskRunId);
            if (!cancelled)
            {
                _logger.LogDebug("No task run with id {TaskRunId} to cancel", taskRunId);
            }
        }
    }
}
