using System;
using NodaTime;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed record VideoFileEntity : ModifiableEntity
{
    public required Guid OwnerId { get; set; }

    public required Guid VideoId { get; set; }

    public required string StoragePath { get; set; }

    public required VideoContainerType Type { get; set; }

    public required int Width { get; set; }

    public required int Height { get; set; }

    public required decimal Framerate { get; set; }

    public Instant? DownloadedAt { get; set; }

    public Guid? DownloadedByUserId { get; set; }
}
