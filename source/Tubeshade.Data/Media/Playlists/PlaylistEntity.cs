using System;
using NodaTime;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media.Playlists;

public record PlaylistEntity : ModifiableEntity, INamedEntity
{
    /// <summary>Gets or sets the library that contains the playlist, and which controls access to it.</summary>
    public required Guid LibraryId { get; set; }

    public required string StoragePath { get; set; }

    public required string ExternalId { get; set; }

    public required string ExternalUrl { get; set; }

    /// <inheritdoc />
    public required string Name { get; set; }

    public required Instant RefreshedAt { get; set; }

    public Instant? SubscribedAt { get; set; }
}
