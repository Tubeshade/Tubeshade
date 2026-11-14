using System;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Playlists;

public sealed class PlaylistParameters : IAccessParameters, IPaginatedParameters, ISortingParameters<SortPlaylistBy>
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
    public required SortPlaylistBy SortBy { get; init; }

    /// <inheritdoc />
    public required SortDirection SortDirection { get; init; }

    public string SortKey => SortBy.Name;

    public bool SortDescending => SortDirection == SortDirection.Descending;
}
