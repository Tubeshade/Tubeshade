using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Tasks;

public interface ITaskPage : IPaginatedDataPage<TaskModel>
{
    Dictionary<string, string?> GetRouteValues(int pageIndex) => new()
    {
        { nameof(PageSize), PageSize?.ToString() },
        { nameof(PageIndex), $"{pageIndex}" },
    };

    Task<IActionResult> OnGet(CancellationToken cancellationToken);

    Task<IActionResult> OnPostScanSubscriptions();

    Task<IActionResult> OnPostScanSegments();

    Task<IActionResult> OnPostUpdateSegments();

    Task<IActionResult> OnPostRetry(Guid taskId);

    Task<IActionResult> OnPostCancel(Guid taskRunId);
}
