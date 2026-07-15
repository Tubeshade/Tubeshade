using System;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Channels;

public sealed class ChannelParameters : IAccessParameters, IPaginatedParameters, ISortingParameters<SortChannelBy>
{
    /// <inheritdoc />
    public Access Access => Access.Read;

    /// <inheritdoc />
    public required int Limit { get; init; }

    /// <inheritdoc />
    public required int Offset { get; init; }

    public required Guid UserId { get; init; }

    public Guid? Id { get; init; }

    public Guid? LibraryId { get; init; }

    public string? Query { get; init; }

    public ExternalAvailability? Availability { get; init; }

    /// <inheritdoc />
    public required SortChannelBy SortBy { get; init; }

    /// <inheritdoc />
    public required SortDirection SortDirection { get; init; }
}
