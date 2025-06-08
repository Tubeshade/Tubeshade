using System;

namespace Tubeshade.Data.Identity;

public sealed class UserLogin
{
    public string ProviderKey { get; init; } = null!;

    public string? ProviderDisplayName { get; init; }

    public Guid UserId { get; init; }

    public string LoginProvider { get; init; } = null!;

    public string? RefreshToken { get; set; }
}
