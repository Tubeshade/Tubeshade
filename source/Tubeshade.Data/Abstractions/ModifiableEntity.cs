using NodaTime;

namespace Tubeshade.Data.Abstractions;

public abstract record ModifiableEntity : Entity, IModifiableEntity
{
    /// <inheritdoc />
    public Instant ModifiedAt { get; set; }

    /// <inheritdoc />
    public long ModifiedByUserId { get; set; }
}
