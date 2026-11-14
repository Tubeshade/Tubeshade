using NodaTime;

namespace Tubeshade.Data.Abstractions;

/// <summary>Represents an entity that can be updated.</summary>
public interface IModifiableEntity : IEntity
{
    Instant ModifiedAt { get; set; }

    long ModifiedByUserId { get; set; }
}
