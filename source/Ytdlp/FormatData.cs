using System.Text.Json.Serialization;

namespace Ytdlp;

public sealed class FormatData
{
    public required string Url { get; init; }

    public object? RequestData { get; init; }

    public string? ManifestUrl { get; init; }

    public int? ManifestStreamNumber { get; init; }

    [JsonPropertyName("ext")]
    public required string Extension { get; init; }

    public required string Format { get; init; }

    public required string FormatId { get; init; }

    public string? FormatNote { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public string? Resolution { get; init; }

    [JsonPropertyName("fps")]
    public decimal? Framerate { get; init; }

    [JsonPropertyName("filesize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("filesize_approx")]
    public long? ApproximateFileSize { get; set; }
}
