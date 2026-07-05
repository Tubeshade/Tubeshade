using System;
using System.Collections.Generic;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Pages.Channels;

public sealed class ChannelModel
{
    public required Guid LibraryId { get; init; }

    public required ChannelEntity Channel { get; init; }

    public required List<ImageFileEntity> Thumbnails { get; init; }

    public required List<ImageFileEntity> Banners { get; init; }

    public int? VideoCount { get; init; }
}
