using System.Text.Json.Serialization;

namespace YoutubeDLSharp.Metadata;
// https://github.com/yt-dlp/yt-dlp/blob/85b33f5c163f60dbd089a6b9bc2ba1366d3ddf93/yt_dlp/extractor/common.py#L105-L534

/// <summary>
/// Represents information for one available download format for one video as extracted by yt-dlp.
/// </summary>
public class FormatData
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("manifest_url")]
    public string? ManifestUrl { get; set; }

    [JsonPropertyName("ext")]
    public required string Extension { get; set; }

    [JsonPropertyName("format")]
    public required string Format { get; set; }

    [JsonPropertyName("format_id")]
    public required string FormatId { get; set; }

    [JsonPropertyName("format_note")]
    public string? FormatNote { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; }

    [JsonPropertyName("dynamic_range")]
    public string? DynamicRange { get; set; }

    [JsonPropertyName("tbr")]
    public double? Bitrate { get; set; }

    [JsonPropertyName("abr")]
    public double? AudioBitrate { get; set; }

    [JsonPropertyName("acodec")]
    public string? AudioCodec { get; set; }

    [JsonPropertyName("asr")]
    public double? AudioSamplingRate { get; set; }

    [JsonPropertyName("audio_channels")]
    public int? AudioChannels { get; set; }

    [JsonPropertyName("vbr")]
    public double? VideoBitrate { get; set; }

    [JsonPropertyName("fps")]
    public float? FrameRate { get; set; }

    [JsonPropertyName("vcodec")]
    public string? VideoCodec { get; set; }

    [JsonPropertyName("container")]
    public string? ContainerFormat { get; set; }

    [JsonPropertyName("filesize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("filesize_approx")]
    public long? ApproximateFileSize { get; set; }

    [JsonPropertyName("player_url")]
    public string? PlayerUrl { get; set; }

    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    [JsonPropertyName("fragment_base_url")]
    public string? FragmentBaseUrl { get; set; }

    [JsonPropertyName("is_from_start")]
    public bool? IsFromStart { get; set; }

    [JsonPropertyName("preference")]
    public int? Preference { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("language_preference")]
    public int? LanguagePreference { get; set; }

    [JsonPropertyName("quality")]
    public double? Quality { get; set; }

    [JsonPropertyName("source_preference")]
    public int? SourcePreference { get; set; }

    [JsonPropertyName("stretched_ratio")]
    public float? StretchedRatio { get; set; }

    [JsonPropertyName("no_resume")]
    public bool? NoResume { get; set; }

    [JsonPropertyName("has_drm")]
    // [JsonConverter(typeof(JsonStringEnumConverter<MaybeBool>))]
    public bool? HasDrm { get; set; }

    public override string ToString() => $"[{Extension}] {Format}";

    //Unused Fields (These are fields that were excluded, but documented for future use:
    //downloader_options (internal use only)
    //fragments
    //http_headers
    //manifest_stream_number (internal use only)
}
