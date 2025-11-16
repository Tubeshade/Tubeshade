using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tubeshade.Server.Pages.Downloads;

public static class PageModelExtensions
{
    public static void ApplyDefaultFilters<TPage>(this TPage page)
        where TPage : PageModel, IDownloadPage
    {
        if (page.WithFiles is null && !page.Request.Query.ContainsKey(nameof(page.WithFiles)))
        {
            page.WithFiles = true;
        }
    }
}
