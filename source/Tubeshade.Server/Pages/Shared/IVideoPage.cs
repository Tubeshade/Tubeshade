using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media;
using Tubeshade.Server.Pages.Libraries;

namespace Tubeshade.Server.Pages.Shared;

public interface IVideoPage : IPaginatedDataPage<VideoModel>, IPageWithSettings
{
    bool? Viewed { get; }

    string? Query { get; }

    VideoType? Type { get; }

    Dictionary<string, string?> GetRouteValues(int pageIndex) => new()
    {
        { nameof(Query), Query },
        { nameof(Viewed), $"{Viewed}" },
        { nameof(Type), Type?.Name },
        { nameof(PageSize), $"{PageSize}" },
        { nameof(PageIndex), $"{pageIndex}" },
    };

    Task<IActionResult> OnPostViewed(string? viewed, Guid videoId);
}
