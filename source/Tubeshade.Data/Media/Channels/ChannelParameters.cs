using System;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Channels;

public sealed class ChannelParameters : IAccessParameters, IPaginatedParameters, ISortingParameters<SortChannelBy>
{
    /// <inheritdoc />
    public Guid PrincipalId => UserId;

    public Permissions Permissions => Permissions.Read;

    /// <inheritdoc />
    public required int Limit { get; init; }

    /// <inheritdoc />
    public required int Offset { get; init; }

    public required Guid UserId { get; init; }

    public Guid? Id { get; init; }

    public Guid? LibraryId { get; init; }

    public Guid? CreatorId { get; set; }

    public string? Query { get; init; }

    public ExternalAvailability? Availability { get; init; }

    /// <inheritdoc />
    public required SortChannelBy SortBy { get; init; }

    /// <inheritdoc />
    public required SortDirection SortDirection { get; init; }

    public string SortKey => SortBy.Name;

    public bool SortDescending => SortDirection == SortDirection.Descending;
}
