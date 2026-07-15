using System;

namespace Tubeshade.Data.Media.Channels;

public sealed record DetailedChannel : ChannelEntity
{
    public required Guid PrimaryLibraryId { get; init; }

    public required int VideoCount { get; init; }

    public required int TotalCount { get; init; }

    internal ImageFileEntity[] Images { get; set; } = [];

    public ImageFileEntity[] Banners { get; internal set; } = [];

    public ImageFileEntity[] Thumbnails { get; internal set; } = [];
}
