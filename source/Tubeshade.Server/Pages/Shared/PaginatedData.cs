using System;
using System.Collections.Generic;
using System.Linq;

namespace Tubeshade.Server.Pages.Shared;

public class PaginatedData<T>
{
    public required Guid? LibraryId { get; init; }

    public required List<T> Data { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public int PageCount => (int)Math.Round((decimal)TotalCount / PageSize, MidpointRounding.ToPositiveInfinity);

    public int StartIndex => Page * PageSize + 1;

    public int EndIndex => (Page + 1) * PageSize; // todo: wrong for last page

    public bool IsFirst => Page is 0;

    public bool IsLast => PageCount is 0 || Page == PageCount - 1;

    public IEnumerable<int> DisplayedPages => PageCount <= 5
        ? Enumerable.Range(1, PageCount)
        : Enumerable.Range(Page - 2, 5);
}
