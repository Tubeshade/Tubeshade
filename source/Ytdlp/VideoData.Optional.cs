using System.Text.Json.Serialization;
using NodaTime;

namespace Ytdlp;

public sealed partial class VideoData
{
    [JsonPropertyName("_type")]
    public string? Type { get; init; }

    public bool? Direct { get; init; }

    [JsonPropertyName("alt_title")]
    public string? AlternativeTitle { get; init; }

    public string? DisplayId { get; init; }

    // public object[]? Thumbnails { get; init; }

    public string? Thumbnail { get; init; }

    public string? Description { get; init; }

    public string? Uploader { get; init; }

    public string? Channel { get; init; }

    public string? ChannelId { get; init; }

    public string? ChannelUrl { get; init; }

    public string? Availability { get; init; }

    [JsonPropertyName("duration")]
    public decimal? DurationInSeconds { get; init; }

    [JsonPropertyName("duration_string")]
    [JsonConverter(typeof(DurationConverter))]
    public Duration? Duration { get; init; }

    public long? ReleaseTimestamp { get; init; }

    public long? Timestamp { get; init; }

    public string? LiveStatus { get; init; }

    public string[]? Categories { get; init; }

    public string[]? Tags { get; init; }

    public long? Viewcount { get; init; }

    public long? LikeCount { get; init; }

    public string? WebpageUrl { get; init; }

    public object[]? Chapters { get; init; }
}
