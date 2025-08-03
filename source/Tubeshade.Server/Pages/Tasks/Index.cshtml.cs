using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Libraries.Tasks;

namespace Tubeshade.Server.Pages.Tasks;

public sealed class Index : PageModel
{
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _libraryRepository;
    private readonly TaskRepository _taskRepository;

    public Index(NpgsqlConnection connection, LibraryRepository libraryRepository, TaskRepository taskRepository)
    {
        _connection = connection;
        _libraryRepository = libraryRepository;
        _taskRepository = taskRepository;
    }

    public List<TaskModel> Tasks { get; set; } = [];

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Tasks = (await _connection.QueryAsync<TaskModel>(new CommandDefinition(
            $"""
             SELECT tasks.id                 AS Id,
                    tasks.type               AS Type,
                    tasks.payload            AS Payload,
                    task_runs.id             AS RunId,
                    task_run_progress.value  AS Value,
                    task_run_progress.target AS Target,
                    task_run_results.result  AS Result,
                    task_run_results.message AS Message
             FROM tasks.tasks
                      INNER JOIN media.libraries ON (tasks.payload::json ->> 'libraryId')::uuid = libraries.id
                      INNER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
                      LEFT OUTER JOIN tasks.task_run_progress ON task_runs.id = task_run_progress.run_id
                      LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
             WHERE libraries.owner_id = @{nameof(userId)}
             ORDER BY task_run_results.created_at DESC, task_runs.created_at DESC, tasks.created_at DESC;
             """,
            new { userId },
            cancellationToken: cancellationToken
        ))).ToList();
    }

    public async Task<IActionResult> OnGetRunning(CancellationToken cancellationToken)
    {
        await OnGet(cancellationToken);
        return Partial("Libraries/Tasks/_RunningTasks", Tasks);
    }

    public async Task<IActionResult> OnPostScanSubscriptions()
    {
        var userId = User.GetUserId();
        var cancellationToken = CancellationToken.None;

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var libraries = await _libraryRepository.GetAsync(userId, transaction);

        foreach (var library in libraries)
        {
            var payload = new ScanSubscriptionsPayload { LibraryId = library.Id, UserId = userId };
            var taskId = await _taskRepository.AddScanSubscriptionsTask(payload, userId, transaction);
            await _taskRepository.TriggerTask(taskId, transaction);
        }

        await transaction.CommitAsync(cancellationToken);

        return StatusCode(StatusCodes.Status204NoContent);
    }

    public async Task<IActionResult> OnPostScanSegments()
    {
        var userId = User.GetUserId();
        var cancellationToken = CancellationToken.None;

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var libraries = await _libraryRepository.GetAsync(userId, transaction);
        foreach (var library in libraries)
        {
            var payload = new ScanSponsorBlockSegmentsPayload { LibraryId = library.Id, UserId = userId };
            var taskId = await _taskRepository.AddScanSegmentsTask(payload, userId, transaction);
            await _taskRepository.TriggerTask(taskId, transaction);
        }

        await transaction.CommitAsync(cancellationToken);

        return StatusCode(StatusCodes.Status204NoContent);
    }
}
