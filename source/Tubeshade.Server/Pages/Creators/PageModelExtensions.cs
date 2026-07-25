using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media.Creators;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Creators;

public static class PageModelExtensions
{
    public static CreatorParameters GetCreatorParameters<TPage>(
        this TPage pageModel,
        Guid userId,
        Guid? libraryId)
        where TPage : PageModel, ICreatorPage
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
            SortBy = pageModel.SortBy ?? Defaults.CreatorOrder,
            SortDirection = pageModel.SortDirection ?? Defaults.CreatorDirection,
        };
    }
}
