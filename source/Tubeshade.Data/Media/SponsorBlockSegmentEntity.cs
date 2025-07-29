using System;
using SponsorBlock;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed record SponsorBlockSegmentEntity : Entity
{
    public required Guid VideoId { get; init; }

    public required string ExternalId { get; init; }

    public required decimal StartTime { get; init; }

    public required decimal EndTime { get; set; }

    public required SegmentCategory Category { get; init; }

    public required SegmentAction Action { get; init; }

    public string? Description { get; init; }
}
