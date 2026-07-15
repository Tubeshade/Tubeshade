using Tubeshade.Data.Media;

namespace Tubeshade.Data.Abstractions;

public interface ISortingParameters<TSortBy>
    where TSortBy : ISortBy
{
    TSortBy SortBy { get; init; }

    SortDirection SortDirection { get; init; }
}
