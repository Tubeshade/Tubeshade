namespace Tubeshade.Data.Abstractions;

/// <summary>Represents an entity that is owned by a user who can control access to it.</summary>
public interface IOwnableEntity : IEntity
{
    long OwnerId { get; set; }
}
