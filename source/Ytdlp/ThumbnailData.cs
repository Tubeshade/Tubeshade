using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ytdlp;

public sealed class ThumbnailData
{
    public string? Id { get; init; }

    public required string Url { get; init; }

    [JsonPropertyName("ext")]
    public string? Extension { get; init; }

    public int? Preference { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public string? Resolution { get; init; }

    public int? Filesize { get; init; }

    public Dictionary<string, string>? HttpHeaders { get; init; }
}
