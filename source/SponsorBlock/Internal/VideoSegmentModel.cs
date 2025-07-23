using System.Text.Json.Serialization;

namespace SponsorBlock.Internal;

internal sealed class VideoSegmentModel
{
    [JsonPropertyName("UUID")]
    public required string Id { get; init; }

    [JsonPropertyName("segment")]
    public required decimal[] Segment { get; init; }

    [JsonPropertyName("category")]
    public required SegmentCategory Category { get; init; }

    [JsonPropertyName("videoDuration")]
    public required decimal VideoDuration { get; init; }

    [JsonPropertyName("actionType")]
    public required SegmentAction Action { get; init; }

    [JsonPropertyName("locked")]
    public required int Locked { get; init; }

    [JsonPropertyName("votes")]
    public required int Votes { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }
}
