using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media.Playlists;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Playlists;

public static class PageModelExtensions
{
    public static PlaylistParameters GetPlaylistParameters<TPage>(
        this TPage pageModel,
        Guid userId,
        Guid? libraryId)
        where TPage : PageModel, IPlaylistPage
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
            SortBy = pageModel.SortBy ?? Defaults.PlaylistOrder,
            SortDirection = pageModel.SortDirection ?? Defaults.PlaylistDirection,
        };
    }
}
