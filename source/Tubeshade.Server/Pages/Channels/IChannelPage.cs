using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Channels;

public interface IChannelPage : IPaginatedDataPage<DetailedChannel>
{
    string? Query { get; set; }

    ExternalAvailability? Availability { get; set; }

    SortChannelBy? SortBy { get; set; }

    SortDirection? SortDirection { get; set; }

    Dictionary<string, string?> GetRouteValues(int pageIndex) => new()
    {
        { nameof(Query), Query },
        { nameof(Availability), Availability?.Name },
        { nameof(SortBy), SortBy?.Name ?? " " },
        { nameof(SortDirection), SortDirection?.Name ?? " " },
        { nameof(PageSize), PageSize?.ToString() },
        { nameof(PageIndex), $"{pageIndex}" },
    };

    void ApplyDefaultFilters<TPage>(TPage page)
        where TPage : PageModel, IChannelPage
    {
        if (page.SortBy is null && !page.Request.Query.ContainsKey(nameof(page.SortBy)))
        {
            page.SortBy = Defaults.ChannelOrder;
        }

        if (page.SortDirection is null && !page.Request.Query.ContainsKey(nameof(page.SortDirection)))
        {
            page.SortDirection = Defaults.ChannelDirection;
        }
    }

    Task<IActionResult> OnPostSubscribe(Guid channelId);

    Task<IActionResult> OnPostUnsubscribe(Guid channelId);

    Task<IActionResult> OnPostScan(Guid channelId, bool? all);
}
