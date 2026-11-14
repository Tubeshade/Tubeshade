using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media.Creators;

public record CreatorEntity : ModifiableEntity, IOwnableEntity, INamedEntity
{
    /// <inheritdoc />
    public long OwnerId { get; set; }

    /// <inheritdoc />
    public required string Name { get; set; }
}
