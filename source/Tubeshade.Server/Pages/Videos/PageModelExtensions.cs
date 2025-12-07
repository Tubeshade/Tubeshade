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
        pageModel.ApplyDefaultFilters(pageModel);

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
}
