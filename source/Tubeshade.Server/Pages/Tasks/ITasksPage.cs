using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Tasks;

public interface ITasksPage : IPaginatedDataPage<TaskModel>, IFormLayout
{
    TaskSource? Source { get; set; }

    TaskStatus? Status { get; set; }

    Dictionary<string, string?> GetRouteValues(int pageIndex) => new()
    {
        { nameof(Source), Source?.Name },
        { nameof(Status), Status?.Name },
        { nameof(PageSize), PageSize?.ToString() },
        { nameof(PageIndex), $"{pageIndex}" },
    };

    Task<IActionResult> OnGet(CancellationToken cancellationToken);

    Task<IActionResult> OnGetTaskRun(Guid taskRunId, CancellationToken cancellationToken);

    Task<IActionResult> OnPostScanSubscriptions();

    Task<IActionResult> OnPostScanSegments();

    Task<IActionResult> OnPostUpdateSegments();

    Task<IActionResult> OnPostRefreshMetadata();

    Task<IActionResult> OnPostRetry(Guid taskId);

    Task<IActionResult> OnPostCancel(Guid taskRunId);
}
