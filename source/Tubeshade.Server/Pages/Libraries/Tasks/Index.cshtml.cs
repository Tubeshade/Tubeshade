using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.Pages.Libraries.Tasks;

public sealed class Index : LibraryPageBase
{
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _libraryRepository;

    public Index(NpgsqlConnection connection, LibraryRepository libraryRepository, TaskRepository taskRepository)
    {
        _connection = connection;
        _libraryRepository = libraryRepository;
    }

    public LibraryEntity Library { get; set; } = null!;

    public List<TaskModel> Tasks { get; set; } = [];

    public async Task OnGet(CancellationToken cancellationToken)
    {
        Library = await _libraryRepository.GetAsync(LibraryId, User.GetUserId(), cancellationToken);
        Tasks = (await _connection.QueryAsync<TaskModel>(new CommandDefinition(
            """
            SELECT tasks.id                 AS Id,
                   tasks.type               AS Type,
                   tasks.payload            AS Payload,
                   task_runs.id             AS RunId,
                   task_run_progress.value  AS Value,
                   task_run_progress.target AS Target,
                   task_run_results.result  AS Result,
                   task_run_results.message AS Message
            FROM tasks.tasks
                     INNER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
                     LEFT OUTER JOIN tasks.task_run_progress ON task_runs.id = task_run_progress.run_id
                     LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
            ORDER BY task_run_results.created_at DESC, task_runs.created_at DESC, tasks.created_at DESC;
            """,
            new { },
            cancellationToken: cancellationToken
        ))).ToList();
    }

    public async Task<IActionResult> OnGetRunning(CancellationToken cancellationToken)
    {
        await OnGet(cancellationToken);
        return Partial("_RunningTasks", Tasks);
    }
}
