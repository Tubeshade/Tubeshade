using System;
using System.Security.Claims;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Identity;

public sealed record ClaimEntity : Entity
{
    /// <summary>
    /// Gets or sets the primary key of the user associated with this claim.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the claim type for this claim.
    /// </summary>
    public string? ClaimType { get; set; }

    /// <summary>
    /// Gets or sets the claim value for this claim.
    /// </summary>
    public string? ClaimValue { get; set; }

    /// <summary>
    /// Converts the entity into a Claim instance.
    /// </summary>
    /// <returns></returns>
    public Claim ToClaim() => new(ClaimType!, ClaimValue!);

    /// <summary>
    /// Reads the type and value from the Claim.
    /// </summary>
    /// <param name="claim"></param>
    public void InitializeFromClaim(Claim claim)
    {
        ClaimType = claim.Type;
        ClaimValue = claim.Value;
    }
}
