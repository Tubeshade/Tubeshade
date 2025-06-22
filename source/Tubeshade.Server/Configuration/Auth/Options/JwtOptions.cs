using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Tubeshade.Server.Configuration.Auth.Options;

/// <summary>Options for built-in user authentication.</summary>
public sealed record JwtOptions
{
    public const string SectionName = "Jwt";

    /// <inheritdoc cref="JwtBearerOptions.Audience"/>
    /// <seealso cref="JwtBearerOptions.Audience"/>
    /// <seealso cref="TokenValidationParameters.ValidAudience"/>
    [Required]
    public string ValidAudience { get; set; } = null!;

    /// <inheritdoc cref="AuthenticationSchemeOptions.ClaimsIssuer"/>
    /// <seealso cref="AuthenticationSchemeOptions.ClaimsIssuer"/>
    /// <seealso cref="TokenValidationParameters.ValidIssuer"/>
    [Required]
    public string ValidIssuer { get; set; } = null!;

    /// <summary>Gets the string value of <see cref="GetSecurityKey"/>.</summary>
    [Required]
    public string Secret { get; set; } = null!;

    /// <seealso cref="TokenValidationParameters.IssuerSigningKey"/>
    public SymmetricSecurityKey GetSecurityKey() => new(Encoding.UTF8.GetBytes(Secret));
}
