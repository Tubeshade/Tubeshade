using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using Tubeshade.Server.Configuration.Auth.Options;

namespace Tubeshade.Server.Configuration.Auth;

internal static class ClaimsPrincipalExtensions
{
    internal static string? GetLoginProvider(this ClaimsPrincipal principal)
    {
        if (principal.Identity?.AuthenticationType is null)
        {
            return null;
        }

        return principal.Identity.AuthenticationType + ServicesCollectionExtensions.OidcSuffix;
    }

    internal static bool TryGetUserId(this ClaimsPrincipal principal, out Guid id)
    {
        id = Guid.Empty;

        var claims = principal.FindAll(ClaimTypes.NameIdentifier).DistinctBy(claim => claim.Value).ToArray();
        if (claims.Length is not 1)
        {
            return false;
        }

        var claim = claims.Single();
        return Guid.TryParseExact(claim.Value, "D", out id);
    }

    internal static void AddUserId(this ClaimsPrincipal principal, Guid userId)
    {
        var identity = new ClaimsIdentity([new Claim(Claims.UserId, userId.ToString("D"))]);
        principal.AddIdentity(identity);
    }

    internal static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindAll(Claims.UserId).DistinctBy(claim => claim.Value).Single();
        return Guid.ParseExact(claim.Value, "D");
    }

    internal static bool TryGetFirstClaimValue(
        this ClaimsPrincipal principal,
        string claimType,
        [NotNullWhen(true)] out string? claimValue)
    {
        var claim = principal.Claims.FirstOrDefault(claim => StringComparer.OrdinalIgnoreCase.Equals(claim.Type, claimType));
        claimValue = claim?.Value;
        return claim is not null;
    }
}
