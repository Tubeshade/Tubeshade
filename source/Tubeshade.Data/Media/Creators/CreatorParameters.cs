using System;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Creators;

public sealed class CreatorParameters : IAccessParameters, IPaginatedParameters, ISortingParameters<SortCreatorBy>
{
    /// <inheritdoc />
    public Access Access => Access.Read;

    /// <inheritdoc />
    public required int Limit { get; init; }

    /// <inheritdoc />
    public required int Offset { get; init; }

    public required Guid UserId { get; init; }

    public Guid? LibraryId { get; init; }

    public string? Query { get; init; }

    /// <inheritdoc />
    public required SortCreatorBy SortBy { get; init; }

    /// <inheritdoc />
    public required SortDirection SortDirection { get; init; }
}
