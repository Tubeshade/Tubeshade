using System.Text.Json.Serialization;

namespace YoutubeDLSharp.Metadata;

//https://github.com/yt-dlp/yt-dlp/blob/9c53b9a1b6b8914e4322263c97c26999f2e5832e/yt_dlp/extractor/common.py#L105-L403
public class SubtitleData
{
    [JsonPropertyName("ext")]
    public required string Ext { get; set; }

    // todo: one of data or url is required
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    //Unused Fields (These are fields that were excluded, but documented for future use:
    //http_headers
}
