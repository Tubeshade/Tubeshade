namespace Tubeshade.Data.Media.Videos;

public sealed record DetailedVideo : VideoEntity
{
    public ImageFileEntity[] Thumbnails { get; internal set; } = [];

    public int? PlaylistOrder { get; set; }
}
