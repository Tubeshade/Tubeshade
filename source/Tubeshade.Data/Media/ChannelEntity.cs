using System;
using NodaTime;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed record ChannelEntity : ModifiableEntity, IOwnableEntity, INamedEntity
{
    /// <inheritdoc />
    public Guid OwnerId { get; set; }

    /// <inheritdoc />
    public required string Name { get; set; }

    public required string StoragePath { get; set; }

    public required string ExternalId { get; set; }

    public Instant? SubscribedAt { get; set; }
}