using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media;
using Tubeshade.Server.Pages.Libraries;

namespace Tubeshade.Server.Pages.Shared;

public interface IDownloadPage : IPaginatedDataPage<VideoModel>
{
    Guid? ChannelId { get; }

    string? Query { get; }

    VideoType? Type { get; }

    List<ChannelEntity> Channels { get; }

    Dictionary<string, string?> GetRouteValues(int pageIndex) => new()
    {
        { nameof(Query), Query },
        { nameof(ChannelId), ChannelId?.ToString() },
        { nameof(Type), Type?.Name },
        { nameof(PageSize), PageSize?.ToString() },
        { nameof(PageIndex), pageIndex.ToString() },
    };

    Task<IActionResult> OnPostStartDownload(Guid videoId);

    Task<IActionResult> OnPostScan(Guid videoId);

    Task<IActionResult> OnPostIgnore(Guid videoId);
}
