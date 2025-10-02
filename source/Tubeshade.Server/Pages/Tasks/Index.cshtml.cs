using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Libraries.Tasks;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Tasks;

public sealed class Index : PageModel
{
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _libraryRepository;
    private readonly TaskService _taskService;

    public Index(
        NpgsqlConnection connection,
        LibraryRepository libraryRepository,
        TaskService taskService)
    {
        _connection = connection;
        _libraryRepository = libraryRepository;
        _taskService = taskService;
    }

    public List<TaskModel> Tasks { get; set; } = [];

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        Tasks = await _taskService.GetGroupedTasks(userId, cancellationToken);
    }

    public async Task<IActionResult> OnGetRunning(CancellationToken cancellationToken)
    {
        await OnGet(cancellationToken);
        return Partial("Libraries/Tasks/_RunningTasks", Tasks);
    }

    public async Task<IActionResult> OnPostScanSubscriptions()
    {
        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var libraries = await _libraryRepository.GetAsync(userId, transaction);
        await _taskService.ScanSubscriptions(userId, libraries.Select(library => library.Id), transaction);
        await transaction.CommitAsync();

        return StatusCode(StatusCodes.Status204NoContent);
    }

    public async Task<IActionResult> OnPostScanSegments()
    {
        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var libraries = await _libraryRepository.GetAsync(userId, transaction);
        await _taskService.ScanSegments(userId, libraries.Select(library => library.Id), transaction);
        await transaction.CommitAsync();

        return StatusCode(StatusCodes.Status204NoContent);
    }

    public async Task<IActionResult> OnPostRetry(Guid taskId)
    {
        await _taskService.RetryTask(User.GetUserId(), taskId);
        return StatusCode(StatusCodes.Status204NoContent);
    }

    public async Task<IActionResult> OnPostCancel(Guid taskRunId)
    {
        await _taskService.CancelTaskRun(User.GetUserId(), taskRunId);
        return StatusCode(StatusCodes.Status204NoContent);
    }
}
