using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tubeshade.Server.Services.Background;

public sealed class TaskCancellationService : BackgroundService
{
    private readonly ILogger<TaskCancellationService> _logger;
    private readonly TaskListenerService _taskListenerService;

    public TaskCancellationService(ILogger<TaskCancellationService> logger, TaskListenerService taskListenerService)
    {
        _logger = logger;
        _taskListenerService = taskListenerService;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _taskListenerService.TaskRunCancelled;

        while (await reader.WaitToReadAsync(stoppingToken))
        {
            var taskRunId = await reader.ReadAsync(stoppingToken);
            _logger.LogDebug("Cancelling task run {TaskRunId}", taskRunId);
            var cancelled = await BackgroundWorkerService.CancelTaskRun(taskRunId);
            if (!cancelled)
            {
                _logger.LogDebug("No task run with id {TaskRunId} to cancel", taskRunId);
            }
        }
    }
}
