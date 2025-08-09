using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Libraries.Tasks;

public sealed class Index : LibraryPageBase
{
    private readonly LibraryRepository _libraryRepository;
    private readonly TaskRepository _taskRepository;
    private readonly TaskService _taskService;

    public Index(
        NpgsqlConnection connection,
        LibraryRepository libraryRepository,
        TaskRepository taskRepository,
        TaskService taskService)
    {
        _libraryRepository = libraryRepository;
        _taskRepository = taskRepository;
        _taskService = taskService;
    }

    public LibraryEntity Library { get; set; } = null!;

    public List<TaskModel> Tasks { get; set; } = [];

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        Library = await _libraryRepository.GetAsync(LibraryId, userId, cancellationToken);
        var tasks = await _taskRepository.GetRunningTasks(LibraryId, userId, cancellationToken);
        Tasks = _taskService.GroupTasks(tasks);
    }

    public async Task<IActionResult> OnGetRunning(CancellationToken cancellationToken)
    {
        await OnGet(cancellationToken);
        return Partial("_RunningTasks", Tasks);
    }

    public async Task<IActionResult> OnPostScanSubscriptions()
    {
        await _taskService.ScanSubscriptions(User.GetUserId(), [LibraryId]);
        return StatusCode(StatusCodes.Status204NoContent);
    }

    public async Task<IActionResult> OnPostScanSegments()
    {
        await _taskService.ScanSegments(User.GetUserId(), [LibraryId]);
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
