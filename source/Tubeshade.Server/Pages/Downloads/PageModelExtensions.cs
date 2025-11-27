using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Downloads;

public static class PageModelExtensions
{
    public static VideoParameters GetVideoParameters<TPage>(
        this TPage pageModel,
        Guid userId,
        Guid? libraryId,
        Guid? channelId)
        where TPage : PageModel, IDownloadPage
    {
        pageModel.ApplyDefaultFilters();

        var pageSize = pageModel.PageSize ?? Defaults.PageSize;
        var page = pageModel.PageIndex ?? Defaults.PageIndex;
        var offset = pageSize * page;

        return new VideoParameters
        {
            UserId = userId,
            LibraryId = libraryId,
            ChannelId = channelId,
            Limit = pageSize,
            Offset = offset,
            Query = pageModel.Query,
            Type = pageModel.Type,
            WithFiles = pageModel.WithFiles,
            Availability = pageModel.Availability,
            SortBy = pageModel.SortBy ?? Defaults.VideoOrder,
            SortDirection = pageModel.SortDirection ?? Defaults.SortDirection,
        };
    }

    private static void ApplyDefaultFilters<TPage>(this TPage page)
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
}
