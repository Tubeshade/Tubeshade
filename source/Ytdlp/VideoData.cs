using System.Text.Json.Serialization;

namespace Ytdlp;

public sealed partial class VideoData
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    // public required object[] Formats { get; init; }

    public string? Url { get; init; } // todo: required in docs, not actually required?

    [JsonPropertyName("ext")]
    public required string Extension { get; init; }

    public required string Format { get; init; }

    public string? PlayerUrl { get; init; } // todo: required in docs, not actually required?
}
