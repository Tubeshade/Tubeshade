using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media.Playlists;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Pages.Videos;

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

    public static VideoParameters GetVideoParameters<TPage>(
        this TPage pageModel,
        Guid userId,
        Guid? libraryId,
        Guid playlistId)
        where TPage : PageModel, IVideoPage
    {
        pageModel.ApplyDefaultFilters(pageModel);

        var pageSize = pageModel.PageSize ?? Defaults.PageSize;
        var page = pageModel.PageIndex ?? Defaults.PageIndex;
        var offset = pageSize * page;

        return new()
        {
            UserId = userId,
            LibraryId = libraryId,
            PlaylistId = playlistId,
            CreatorId = pageModel.CreatorId,
            ChannelId = null,
            Limit = pageSize,
            Offset = offset,
            Viewed = pageModel.Viewed,
            Query = pageModel.Query,
            Type = pageModel.Type,
            WithFiles = pageModel.WithFiles,
            Availability = pageModel.Availability,
            SortBy = pageModel.SortBy ?? Defaults.VideoOrder,
            SortDirection = pageModel.SortDirection ?? Defaults.VideoDirection,
        };
    }
}
