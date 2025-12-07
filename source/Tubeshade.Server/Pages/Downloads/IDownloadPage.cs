using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Pages.Videos;

namespace Tubeshade.Server.Pages.Downloads;

public interface IDownloadPage : IPaginatedDataPage<VideoModel>
{
    Guid? ChannelId { get; set; }

    string? Query { get; set; }

    VideoType? Type { get; set; }

    bool? WithFiles { get; set; }

    ExternalAvailability? Availability { get; set; }

    List<ChannelEntity> Channels { get; }

    public SortVideoBy? SortBy { get; set; }

    public SortDirection? SortDirection { get; set; }

    Dictionary<string, string?> GetRouteValues(int pageIndex) => new()
    {
        { nameof(Query), Query },
        { nameof(ChannelId), ChannelId?.ToString() },
        { nameof(Type), Type?.Name },
        { nameof(WithFiles), WithFiles?.ToString() ?? " " },
        { nameof(Availability), Availability?.Name },
        { nameof(SortBy), SortBy?.Name ?? " " },
        { nameof(SortDirection), SortDirection?.Name ?? " " },
        { nameof(PageSize), PageSize?.ToString() },
        { nameof(PageIndex), pageIndex.ToString() },
    };

    void ApplyDefaultFilters<TPage>(TPage page)
        where TPage : PageModel, IDownloadPage
    {
        if (page.WithFiles is null && !page.Request.Query.ContainsKey(nameof(page.WithFiles)))
        {
            page.WithFiles = true;
        }

        if (page.SortBy is null && !page.Request.Query.ContainsKey(nameof(page.SortBy)))
        {
            page.SortBy = Defaults.VideoOrder;
        }

        if (page.SortDirection is null && !page.Request.Query.ContainsKey(nameof(page.SortDirection)))
        {
            page.SortDirection = Defaults.SortDirection;
        }
    }

    Task<IActionResult> OnPostStartDownload(Guid videoId);

    Task<IActionResult> OnPostScan(Guid videoId);

    Task<IActionResult> OnPostIgnore(Guid videoId);
}
