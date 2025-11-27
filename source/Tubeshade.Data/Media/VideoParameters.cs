using System;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media;

public sealed class VideoParameters : IAccessParameters, IPaginatedParameters
{
    public required Guid UserId { get; init; }

    /// <inheritdoc />
    public Access Access => Access.Read;

    public Guid? LibraryId { get; init; }

    public Guid? ChannelId { get; init; }

    /// <inheritdoc />
    public required int Limit { get; init; }

    /// <inheritdoc />
    public required int Offset { get; init; }

    public bool? Viewed { get; init; }

    public string? Query { get; init; }

    public VideoType? Type { get; init; }

    public bool? WithFiles { get; init; }

    public ExternalAvailability? Availability { get; init; }

    public required SortVideoBy SortBy { get; init; }

    public required SortDirection SortDirection { get; init; }
}
