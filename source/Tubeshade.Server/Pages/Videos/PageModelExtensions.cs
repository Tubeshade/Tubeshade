using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tubeshade.Server.Pages.Videos;

public static class PageModelExtensions
{
    public static void ApplyDefaultFilters<TPage>(this TPage page)
        where TPage : PageModel, IVideoPage
    {
        if (page.WithFiles is null && !page.Request.Query.ContainsKey(nameof(page.WithFiles)))
        {
            page.WithFiles = true;
        }

        if (page.Viewed is null && !page.Request.Query.ContainsKey(nameof(page.Viewed)))
        {
            page.Viewed = false;
        }
    }
}
