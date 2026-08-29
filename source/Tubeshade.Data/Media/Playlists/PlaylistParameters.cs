using System;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Playlists;

public sealed class PlaylistParameters : IAccessParameters, IPaginatedParameters, ISortingParameters<SortPlaylistBy>
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
    public required SortPlaylistBy SortBy { get; init; }

    /// <inheritdoc />
    public required SortDirection SortDirection { get; init; }
}
