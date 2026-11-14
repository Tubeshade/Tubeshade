using Tubeshade.Data.Abstractions;
using Tubeshade.Data.Identity;

namespace Tubeshade.Data.AccessControl;

/// <summary>Owns entities; principals reach those entities through grants on the owner.</summary>
/// <seealso cref="UserEntity"/>
public sealed record OwnerEntity : Entity, INamedEntity
{
    /// <inheritdoc />
    public required string Name { get; set; }

    public long? PersonalFor { get; init; }
}
