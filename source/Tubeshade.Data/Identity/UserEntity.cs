using System;
using Microsoft.AspNetCore.Identity;
using NodaTime;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Identity;

/// <summary>A user within the context of this application.</summary>
public sealed record UserEntity : Entity, IModifiableEntity, INamedEntity
{
    /// <inheritdoc />
    public Instant ModifiedAt { get; set; }

    /// <inheritdoc />
    public Guid ModifiedByUserId { get; set; }

    /// <inheritdoc />
    public Access AccessFoo => Access.Modify;

    /// <inheritdoc />
    [ProtectedPersonalData]
    public required string Name { get; set; }

    /// <inheritdoc />
    [ProtectedPersonalData]
    public required string NormalizedName { get; set; }

    [ProtectedPersonalData]
    public string? FullName { get; set; }

    [ProtectedPersonalData]
    public required string Email { get; set; }

    [ProtectedPersonalData]
    public required string NormalizedEmail { get; set; }

    [PersonalData]
    public bool EmailConfirmed { get; set; }

    public byte[]? PasswordHash { get; set; }

    public Guid? SecurityStamp { get; set; }

    public Guid? ConcurrencyStamp { get; set; } = Guid.NewGuid();

    [PersonalData]
    public bool TwoFactorEnabled { get; set; }

    public Instant? LockoutEnd { get; set; }

    public bool LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public required string TimeZoneId { get; set; }

    public void NormalizeInvariant()
    {
        // todo: Identity framework already does case normalization
        NormalizedName = Name.NormalizeInvariant(false);
        NormalizedEmail = Email.NormalizeInvariant(false);
    }
}
