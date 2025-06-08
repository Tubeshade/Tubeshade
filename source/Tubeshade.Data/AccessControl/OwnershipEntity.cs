using System;
using NodaTime;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.Identity;

namespace Tubeshade.Data.AccessControl;

/// <summary>
/// Link between <see cref="OwnerEntity"/> and the entities which represent the user,
/// for example <see cref="UserEntity"/>.
/// </summary>
public sealed record OwnershipEntity : Entity, IModifiableEntity
{
    /// <inheritdoc />
    public required Instant ModifiedAt { get; set; }

    /// <inheritdoc />
    public required Guid ModifiedByUserId { get; set; }

    public required Guid OwnerId { get; set; }

    public required Guid UserId { get; set; }

    public required Access AccessFoo { get; set; }
}
