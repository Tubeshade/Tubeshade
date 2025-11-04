using System;
using System.Threading;
using System.Threading.Tasks;
using Htmx;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Pages.Tasks;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Libraries.Tasks;

public sealed class Index : LibraryPageBase, ITaskPage
{
    private readonly LibraryRepository _libraryRepository;
    private readonly TaskService _taskService;

    public Index(LibraryRepository libraryRepository, TaskService taskService)
    {
        _libraryRepository = libraryRepository;
        _taskService = taskService;
    }

    public LibraryEntity Library { get; set; } = null!;

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageIndex { get; set; }

    /// <inheritdoc />
    public PaginatedData<TaskModel> PageData { get; set; } = null!;

    /// <inheritdoc />
    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var pageSize = PageSize ?? Defaults.PageSize;
        var page = PageIndex ?? Defaults.PageIndex;
        var offset = pageSize * page;

        Library = await _libraryRepository.GetAsync(LibraryId, userId, cancellationToken);
        var tasks = await _taskService.GetGroupedTasks(
            new TaskParameters
            {
                UserId = userId,
                LibraryId = LibraryId,
                Limit = pageSize,
                Offset = offset,
            },
            cancellationToken);

        var totalCount = tasks is [] ? 0 : tasks[0].TotalCount;

        PageData = new PaginatedData<TaskModel>
        {
            LibraryId = LibraryId,
            Data = tasks,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };

        return Request.IsHtmx()
            ? Partial("Tasks/_RunningTasks", this)
            : Page();
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
