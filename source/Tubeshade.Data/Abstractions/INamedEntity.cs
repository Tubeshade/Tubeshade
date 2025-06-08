namespace Tubeshade.Data.Abstractions;

/// <summary>Represents an entity that has a name that must be unique within some scope.</summary>
public interface INamedEntity : IEntity
{
    string Name { get; set; }
}
