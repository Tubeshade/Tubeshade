namespace Tubeshade.Data.Media.Creators;

public sealed record DetailedCreator : CreatorEntity
{
    public required int TotalCount { get; init; }

    public required int ChannelCount { get; init; }

    public ImageFileEntity[] Thumbnails { get; internal set; } = [];
}
