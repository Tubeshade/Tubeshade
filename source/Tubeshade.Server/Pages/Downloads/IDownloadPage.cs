using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Pages.Videos;

namespace Tubeshade.Server.Pages.Downloads;

public interface IDownloadPage : IPaginatedDataPage<VideoModel>
{
    Guid? ChannelId { get; }

    string? Query { get; }

    VideoType? Type { get; }

    bool? WithFiles { get; }

    ExternalAvailability? Availability { get; }

    List<ChannelEntity> Channels { get; }

    Dictionary<string, string?> GetRouteValues(int pageIndex) => new()
    {
        { nameof(Query), Query },
        { nameof(ChannelId), ChannelId?.ToString() },
        { nameof(Type), Type?.Name },
        { nameof(WithFiles), WithFiles?.ToString() },
        { nameof(Availability), Availability?.Name },
        { nameof(PageSize), PageSize?.ToString() },
        { nameof(PageIndex), pageIndex.ToString() },
    };

    Task<IActionResult> OnPostStartDownload(Guid videoId);

    Task<IActionResult> OnPostScan(Guid videoId);

    Task<IActionResult> OnPostIgnore(Guid videoId);
}
