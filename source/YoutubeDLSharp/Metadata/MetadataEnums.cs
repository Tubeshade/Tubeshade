using System.Text.Json.Serialization;

namespace YoutubeDLSharp.Metadata;
//https://github.com/yt-dlp/yt-dlp/blob/9c53b9a1b6b8914e4322263c97c26999f2e5832e/yt_dlp/extractor/common.py#L105-L403

/// <summary>
/// Possible types of media fetched by yt-dlp.
/// </summary>
public enum MetadataType
{
    [JsonStringEnumMemberName("video")] Video,
    [JsonStringEnumMemberName("playlist")] Playlist,
    [JsonStringEnumMemberName("multi_video")] MultiVideo,
    [JsonStringEnumMemberName("url")] Url,

    [JsonStringEnumMemberName("url_transparent")]
    UrlTransparent
}

public enum LiveStatus
{
    [JsonStringEnumMemberName("unknown")] None,
    [JsonStringEnumMemberName("is_live")] IsLive,
    [JsonStringEnumMemberName("is_upcoming")] IsUpcoming,
    [JsonStringEnumMemberName("was_live")] WasLive,
    [JsonStringEnumMemberName("not_live")] NotLive,
    [JsonStringEnumMemberName("post_live")] PostLive
}

public enum Availability
{
    [JsonStringEnumMemberName("private")] Private,
    [JsonStringEnumMemberName("premium_only")] PremiumOnly,

    [JsonStringEnumMemberName("subscriber_only")]
    SubscriberOnly,
    [JsonStringEnumMemberName("needs_auth")] NeedsAuth,
    [JsonStringEnumMemberName("unlisted")] Unlisted,
    [JsonStringEnumMemberName("public")] Public
}

public enum MaybeBool
{
    [JsonStringEnumMemberName("False")] False,
    [JsonStringEnumMemberName("maybe")] Maybe,
    [JsonStringEnumMemberName("True")] True
}
