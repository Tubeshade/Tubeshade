using System;
using System.Collections.Generic;
using System.Linq;

namespace Tubeshade.Server.Pages.Shared;

public sealed class PaginatedData<T>
{
    private const int DisplayedPageCount = 5;

    public required Guid? LibraryId { get; init; }

    public required List<T> Data { get; init; }

    public required int Page { get; set; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public int PageCount => (int)Math.Round((decimal)TotalCount / PageSize, MidpointRounding.ToPositiveInfinity);

    public int StartIndex => Page * PageSize + 1;

    public int EndIndex
    {
        get
        {
            if (PageCount is 0)
            {
                return 0;
            }

            if (Page + 1 == PageCount)
            {
                return TotalCount;
            }

            return (Page + 1) * PageSize;
        }
    }

    public bool IsFirst => Page is 0;

    public bool IsLast => PageCount is 0 || Page == PageCount - 1;

    public IEnumerable<int> DisplayedPages
    {
        get
        {
            if (PageCount <= DisplayedPageCount)
            {
                return Enumerable.Range(1, PageCount);
            }

            if (Page + 1 <= DisplayedPageCount / 2)
            {
                return Enumerable.Range(1, DisplayedPageCount);
            }

            if (Page >= PageCount - DisplayedPageCount / 2)
            {
                return Enumerable.Range(PageCount - DisplayedPageCount + 1, DisplayedPageCount);
            }

            return Enumerable.Range(Page - 1, DisplayedPageCount);
        }
    }
}
