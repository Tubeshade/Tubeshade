using System;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed record LibraryEntity : ModifiableEntity, IOwnableEntity, INamedEntity
{
    /// <inheritdoc />
    public Guid OwnerId { get; set; }

    /// <inheritdoc />
    public required string Name { get; set; }

    public required string StoragePath { get; set; }
}
