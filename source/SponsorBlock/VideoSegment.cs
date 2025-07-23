namespace SponsorBlock;

public sealed class VideoSegment
{
    public required string Id { get; init; }

    public required decimal StartTime { get; init; }

    public required decimal EndTime { get; init; }

    public required SegmentCategory Category { get; init; }

    public required decimal VideoDuration { get; init; }

    public required SegmentAction Action { get; init; }

    public required bool Locked { get; init; }

    public required int Votes { get; init; }

    public string? Description { get; init; }
}
