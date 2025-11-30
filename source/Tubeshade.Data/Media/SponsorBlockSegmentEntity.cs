using System;
using SponsorBlock;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed record SponsorBlockSegmentEntity : ModifiableEntity
{
    public required Guid VideoId { get; init; }

    public required string ExternalId { get; init; }

    public required decimal StartTime { get; set; }

    public required decimal EndTime { get; set; }

    public required SegmentCategory Category { get; set; }

    public required SegmentAction Action { get; set; }

    public required bool Locked { get; set; }

    public string? Description { get; set; }
}
