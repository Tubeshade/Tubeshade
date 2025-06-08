using Tubeshade.Data.Abstractions;
using Tubeshade.Data.Identity;

namespace Tubeshade.Data.AccessControl;

/// <summary>Represents a collection of other entities (users, roles, groups, etc.) that can own other entities.</summary>
/// <seealso cref="OwnershipEntity"/>
/// <seealso cref="UserEntity"/>
public sealed record OwnerEntity : Entity, INamedEntity
{
    /// <inheritdoc />
    public required string Name { get; set; }
}
