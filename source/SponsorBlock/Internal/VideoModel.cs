using System.Text.Json.Serialization;

namespace SponsorBlock.Internal;

internal sealed class VideoModel
{
    [JsonPropertyName("videoID")]
    public required string VideoId { get; init; }

    [JsonPropertyName("segments")]
    public required VideoSegmentModel[] Segments { get; init; }
}
