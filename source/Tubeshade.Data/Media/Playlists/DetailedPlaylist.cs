namespace Tubeshade.Data.Media.Playlists;

public sealed record DetailedPlaylist : PlaylistEntity
{
    public required int TotalCount { get; init; }

    public required int VideoCount { get; init; }

    public ImageFileEntity[] Thumbnails { get; internal set; } = [];
}
