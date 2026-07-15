using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Channels;

public static class PageModelExtensions
{
    public static ChannelParameters GetChannelParameters<TPage>(
        this TPage pageModel,
        Guid userId,
        Guid? libraryId)
        where TPage : PageModel, IChannelPage
    {
        pageModel.ApplyDefaultFilters(pageModel);

        var pageSize = pageModel.PageSize ?? Defaults.PageSize;
        var page = pageModel.PageIndex ?? Defaults.PageIndex;
        var offset = pageSize * page;

        return new()
        {
            UserId = userId,
            LibraryId = libraryId,
            Limit = pageSize,
            Offset = offset,
            Query = pageModel.Query,
            Availability = pageModel.Availability,
            SortBy = pageModel.SortBy ?? Defaults.ChannelOrder,
            SortDirection = pageModel.SortDirection ?? Defaults.ChannelDirection,
        };
    }
}
