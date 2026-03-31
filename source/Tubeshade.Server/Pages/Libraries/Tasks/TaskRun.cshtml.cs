using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Pages.Tasks;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Libraries.Tasks;

public sealed class TaskRun : LibraryPageBase, ITaskRunPage
{
    private readonly LibraryRepository _libraryRepository;
    private readonly TaskService _taskService;

    public TaskRun(LibraryRepository libraryRepository, TaskService taskService)
    {
        _libraryRepository = libraryRepository;
        _taskService = taskService;
    }

    public LibraryEntity Library { get; set; } = null!;

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public Guid TaskRunId { get; set; }

    /// <inheritdoc />
    public TaskModel Task { get; private set; } = null!;

    /// <inheritdoc />
    public TaskRunModel Run { get; private set; } = null!;

    /// <inheritdoc />
    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        Library = await _libraryRepository.GetAsync(LibraryId, userId, cancellationToken);

        Task = await _taskService.GetGroupedTask(
            new TaskParameters
            {
                UserId = userId,
                LibraryId = LibraryId,
                TaskRunId = TaskRunId,
                Limit = Defaults.PageSize,
                Offset = 0,
            },
            cancellationToken);

        Run = Task.Runs.Single(task => task.Id == TaskRunId);

        return Page();
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostRetry(Guid taskId)
    {
        await _taskService.RetryTask(User.GetUserId(), taskId, TaskSource.User);
        return StatusCode(StatusCodes.Status204NoContent);
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostCancel(Guid taskRunId)
    {
        await _taskService.CancelTaskRun(User.GetUserId(), taskRunId);
        return StatusCode(StatusCodes.Status204NoContent);
    }
}
