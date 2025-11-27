using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Videos;

public interface IVideoPage : IPaginatedDataPage<VideoModel>
{
    bool? Viewed { get; set; }

    string? Query { get; set; }

    VideoType? Type { get; set; }

    bool? WithFiles { get; set; }

    ExternalAvailability? Availability { get; set; }

    public SortVideoBy? SortBy { get; set; }

    public SortDirection? SortDirection { get; set; }

    Dictionary<string, string?> GetRouteValues(int pageIndex) => new()
    {
        { nameof(Query), Query },
        { nameof(Viewed), Viewed?.ToString() },
        { nameof(Type), Type?.Name },
        { nameof(WithFiles), WithFiles?.ToString() },
        { nameof(Availability), Availability?.Name },
        { nameof(SortBy), SortBy?.Name },
        { nameof(SortDirection), SortDirection?.Name },
        { nameof(PageSize), PageSize?.ToString() },
        { nameof(PageIndex), $"{pageIndex}" },
    };

    Task<IActionResult> OnPostViewed(string? viewed, Guid videoId);
}
