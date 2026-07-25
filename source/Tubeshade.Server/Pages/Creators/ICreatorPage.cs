using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Creators;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Creators;

public interface ICreatorPage : IPaginatedDataPage<DetailedCreator>
{
    string? Query { get; set; }

    SortCreatorBy? SortBy { get; set; }

    SortDirection? SortDirection { get; set; }

    Dictionary<string, string?> GetRouteValues(int pageIndex) => new()
    {
        { nameof(Query), Query },
        { nameof(SortBy), SortBy?.Name ?? " " },
        { nameof(SortDirection), SortDirection?.Name ?? " " },
        { nameof(PageSize), PageSize?.ToString() },
        { nameof(PageIndex), $"{pageIndex}" },
    };

    void ApplyDefaultFilters<TPage>(TPage page)
        where TPage : PageModel, ICreatorPage
    {
        if (page.SortBy is null && !page.Request.Query.ContainsKey(nameof(page.SortBy)))
        {
            page.SortBy = Defaults.CreatorOrder;
        }

        if (page.SortDirection is null && !page.Request.Query.ContainsKey(nameof(page.SortDirection)))
        {
            page.SortDirection = Defaults.CreatorDirection;
        }
    }
}
