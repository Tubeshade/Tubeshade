using System;
using NodaTime;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed record VideoEntity : ModifiableEntity, IOwnableEntity, INamedEntity
{
    /// <inheritdoc />
    public Guid OwnerId { get; set; }

    /// <inheritdoc />
    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string[] Categories { get; set; }

    public required string[] Tags { get; set; }

    public long? ViewCount { get; set; }

    public long? LikeCount { get; set; }

    public required Guid ChannelId { get; set; }

    public required string StoragePath { get; set; }

    public required string ExternalId { get; set; }

    public required string ExternalUrl { get; set; }

    public required Instant PublishedAt { get; set; }

    public required Instant RefreshedAt { get; set; }

    public required ExternalAvailability Availability { get; set; }

    public required Period Duration { get; set; }

    public Instant? IgnoredAt { get; set; }

    public Guid? IgnoredByUserId { get; set; }

    public Instant? ViewedAt { get; set; }

    public int TotalCount { get; init; }
}
