using System.Text.Json.Serialization;
using NodaTime;

namespace Ytdlp;

public sealed class VideoData
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required FormatData[] Formats { get; init; }

    public string? Url { get; init; } // todo: required in docs, not actually required?

    [JsonPropertyName("ext")]
    public required string Extension { get; init; }

    public required string Format { get; init; }

    public string? PlayerUrl { get; init; } // todo: required in docs, not actually required?

    // Optional fields

    [JsonPropertyName("_type")]
    public string? Type { get; init; }

    public bool? Direct { get; init; }

    [JsonPropertyName("alt_title")]
    public string? AlternativeTitle { get; init; }

    public string? DisplayId { get; init; }

    public ThumbnailData[]? Thumbnails { get; init; }

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

    public ChapterData[]? Chapters { get; init; }
}
