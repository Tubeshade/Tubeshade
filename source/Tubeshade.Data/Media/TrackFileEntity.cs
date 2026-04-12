using System;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed record TrackFileEntity : ModifiableEntity
{
    public required Guid VideoId { get; set; }

    public required string StoragePath { get; set; }

    public required TrackType Type { get; set; }

    public string? Language { get; set; }

    public required byte[] Hash { get; set; }

    public required HashAlgorithm HashAlgorithm { get; set; }

    public required long StorageSize { get; set; }
}
