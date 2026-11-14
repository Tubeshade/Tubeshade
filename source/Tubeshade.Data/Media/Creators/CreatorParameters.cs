using System;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Creators;

public sealed class CreatorParameters : IAccessParameters, IPaginatedParameters, ISortingParameters<SortCreatorBy>
{
    /// <inheritdoc />
    public long PrincipalId => UserId;

    public Permissions Permissions => Permissions.Read;

    /// <inheritdoc />
    public required int Limit { get; init; }

    /// <inheritdoc />
    public required int Offset { get; init; }

    public required long UserId { get; init; }

    public Guid? LibraryId { get; init; }

    public string? Query { get; init; }

    /// <inheritdoc />
    public required SortCreatorBy SortBy { get; init; }

    /// <inheritdoc />
    public required SortDirection SortDirection { get; init; }

    public string SortKey => SortBy.Name;

    public bool SortDescending => SortDirection == SortDirection.Descending;
}
