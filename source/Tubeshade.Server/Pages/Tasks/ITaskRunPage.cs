using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Tasks;

public interface ITaskRunPage : IFormLayout
{
    Guid TaskRunId { get; set; }

    TaskModel Task { get; }

    TaskRunModel Run { get; }

    Task<IActionResult> OnGet(CancellationToken cancellationToken);

    Task<IActionResult> OnPostRetry(Guid taskId);

    Task<IActionResult> OnPostCancel(Guid taskRunId);
}
