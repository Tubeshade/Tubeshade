namespace Tubeshade.Server.Configuration.Auth;

internal static class Schemes
{
    /// <summary>The scheme used to identify application authentication cookies.</summary>
    internal const string Application = $"{Prefix}.Application";

    /// <summary>The scheme used to identify external authentication cookies.</summary>
    internal const string External = $"{Prefix}.External";

    /// <summary>The scheme used to identify Two Factor authentication cookies for saving the Remember Me state.</summary>
    internal const string TwoFactorRememberMe = $"{Prefix}.TwoFactorRememberMe";

    /// <summary>The scheme used to identify Two Factor authentication cookies for round tripping user identities.</summary>
    internal const string TwoFactorUserId = $"{Prefix}.TwoFactorUserId";

    internal const string Bearer = "Bearer";

    private const string Prefix = "Identity";
}
