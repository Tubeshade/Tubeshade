using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Creators;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Videos;

public interface IVideoPage : IPaginatedDataPage<VideoModel>
{
    ViewStatus? Viewed { get; set; }

    string? Query { get; set; }

    VideoType? Type { get; set; }

    bool? WithFiles { get; set; }

    ExternalAvailability? Availability { get; set; }

    Guid? CreatorId { get; set; }

    SortVideoBy? SortBy { get; set; }

    SortDirection? SortDirection { get; set; }

    IReadOnlyList<CreatorEntity> Creators { get; }

    Dictionary<string, string?> GetRouteValues(int pageIndex) => new()
    {
        { nameof(Query), Query },
        { nameof(Viewed), Viewed?.ToString() ?? " " },
        { nameof(Type), Type?.Name },
        { nameof(WithFiles), WithFiles?.ToString() ?? " " },
        { nameof(Availability), Availability?.Name },
        { nameof(CreatorId), CreatorId?.ToString() },
        { nameof(SortBy), SortBy?.Name ?? " " },
        { nameof(SortDirection), SortDirection?.Name ?? " " },
        { nameof(PageSize), PageSize?.ToString() },
        { nameof(PageIndex), $"{pageIndex}" },
    };

    void ApplyDefaultFilters<TPage>(TPage page)
        where TPage : PageModel, IVideoPage
    {
        if (page.WithFiles is null && !page.Request.Query.ContainsKey(nameof(page.WithFiles)))
        {
            page.WithFiles = true;
        }

        if (page.Viewed is null && !page.Request.Query.ContainsKey(nameof(page.Viewed)))
        {
            page.Viewed = page is IPlaylistVideoPage ? null : ViewStatus.NotViewed;
        }

        if (page.SortBy is null && !page.Request.Query.ContainsKey(nameof(page.SortBy)))
        {
            page.SortBy = page is IPlaylistVideoPage ? Defaults.PlaylistVideoOrder : Defaults.VideoOrder;
        }

        if (page.SortDirection is null && !page.Request.Query.ContainsKey(nameof(page.SortDirection)))
        {
            page.SortDirection = page is IPlaylistVideoPage ? Defaults.PlaylistVideoDirection : Defaults.VideoDirection;
        }
    }

    Task<IActionResult> OnPostViewed(string? viewed, Guid videoId);
}
