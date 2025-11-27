using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Videos;

public static class PageModelExtensions
{
    public static VideoParameters GetVideoParameters<TPage>(
        this TPage pageModel,
        Guid userId,
        Guid? libraryId,
        Guid? channelId)
        where TPage : PageModel, IVideoPage
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
            Viewed = pageModel.Viewed,
            Query = pageModel.Query,
            Type = pageModel.Type,
            WithFiles = pageModel.WithFiles,
            Availability = pageModel.Availability,
            SortBy = pageModel.SortBy ?? Defaults.VideoOrder,
            SortDirection = pageModel.SortDirection ?? Defaults.SortDirection,
        };
    }

    private static void ApplyDefaultFilters<TPage>(this TPage page)
        where TPage : PageModel, IVideoPage
    {
        if (page.WithFiles is null && !page.Request.Query.ContainsKey(nameof(page.WithFiles)))
        {
            page.WithFiles = true;
        }

        if (page.Viewed is null && !page.Request.Query.ContainsKey(nameof(page.Viewed)))
        {
            page.Viewed = false;
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
