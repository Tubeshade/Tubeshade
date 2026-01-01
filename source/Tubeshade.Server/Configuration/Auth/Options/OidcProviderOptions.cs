using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Tubeshade.Server.Configuration.Auth.Options;

/// <summary>Options for configuring an OIDC provider.</summary>
public sealed class OidcProviderOptions
{
    /// <summary>The name of the section under which to place provider sections.</summary>
    public const string SectionName = "Oidc";

    /// <inheritdoc cref="JwtBearerOptions.Authority"/>
    /// <seealso cref="JwtBearerOptions.Authority"/>
    [Required]
    public Uri ServerRealm { get; set; } = null!;

    /// <inheritdoc cref="JwtBearerOptions.MetadataAddress"/>
    /// <seealso cref="JwtBearerOptions.MetadataAddress"/>
    [Required]
    public Uri Metadata { get; set; } = null!;

    /// <inheritdoc cref="JwtBearerOptions.Audience"/>
    /// <seealso cref="JwtBearerOptions.Audience"/>
    /// <seealso cref="OpenIdConnectOptions.ClientId"/>
    [Required]
    public string ClientId { get; set; } = null!;

    /// <inheritdoc cref="OpenIdConnectOptions.ClientSecret"/>
    /// <seealso cref="OpenIdConnectOptions.ClientSecret"/>
    public string? ClientSecret { get; set; }

    /// <inheritdoc cref="JwtBearerOptions.RequireHttpsMetadata"/>
    /// <seealso cref="JwtBearerOptions.RequireHttpsMetadata"/>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Gets a display name for the authentication handler. Defaults to the section name if not specified.</summary>
    public string? DisplayName { get; set; }
}
