using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Playlists;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Playlists;

public interface IPlaylistPage : IPaginatedDataPage<DetailedPlaylist>
{
    string? Query { get; set; }

    SortPlaylistBy? SortBy { get; set; }

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
        where TPage : PageModel, IPlaylistPage
    {
        if (page.SortBy is null && !page.Request.Query.ContainsKey(nameof(page.SortBy)))
        {
            page.SortBy = Defaults.PlaylistOrder;
        }

        if (page.SortDirection is null && !page.Request.Query.ContainsKey(nameof(page.SortDirection)))
        {
            page.SortDirection = Defaults.PlaylistDirection;
        }
    }
}
