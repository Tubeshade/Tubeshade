using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media;
using Tubeshade.Server.Pages.Libraries;

namespace Tubeshade.Server.Pages.Shared;

public interface IVideoPage : IPaginatedDataPage<VideoModel>
{
    bool? Viewed { get; }

    string? Query { get; }

    VideoType? Type { get; }

    bool? WithFiles { get; }

    ExternalAvailability? Availability { get; }

    Dictionary<string, string?> GetRouteValues(int pageIndex) => new()
    {
        { nameof(Query), Query },
        { nameof(Viewed), Viewed?.ToString() },
        { nameof(Type), Type?.Name },
        { nameof(WithFiles), WithFiles?.ToString() },
        { nameof(Availability), Availability?.Name },
        { nameof(PageSize), PageSize?.ToString() },
        { nameof(PageIndex), $"{pageIndex}" },
    };

    Task<IActionResult> OnPostViewed(string? viewed, Guid videoId);
}
