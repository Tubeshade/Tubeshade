using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Tubeshade.Server.Configuration.Auth.Options;

namespace Tubeshade.Server.Configuration.Auth;

internal static class ServicesCollectionExtensions
{
    internal const string OidcSuffix = "_oidc";

    internal static IServiceCollection AddAuthenticationAndAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<JwtSecurityTokenHandler>();

        var issuers = new Dictionary<string, string>();
        var authenticationSchemes = new List<string>();

        var jwtOptionsDefined = configuration.GetValidIfDefined<JwtOptions>(out var jwtOptions);
        if (jwtOptionsDefined)
        {
            issuers.Add(jwtOptions!.ValidIssuer, Schemes.Bearer);
            authenticationSchemes.Add(Schemes.Bearer);
        }

        var oidcProviderSection = configuration.GetSection(OidcProviderOptions.OidcProviderSectionName);
        var oidcProviderNames = oidcProviderSection.GetChildren().Select(section => section.Key).ToArray();

        authenticationSchemes.AddRange(oidcProviderNames);

        // This allows to use cookies to authenticate API requests as well
        authenticationSchemes.Add(Schemes.Application);

        var authenticationBuilder =
            services
                .AddAuthorization(options =>
                {
                    var schemes = authenticationSchemes.ToArray();

                    options.AddPolicy(
                        Policies.User,
                        policy => policy
                            .AddAuthenticationSchemes(schemes)
                            .AddRequirements(new UserRequirement()));

                    options.AddPolicy(
                        Policies.Administrator,
                        policy => policy
                            .AddAuthenticationSchemes(schemes)
                            .AddRequirements(new UserRequirement())
                            .AddRequirements(new AdministratorRoleRequirement()));

                    options.AddPolicy(
                        Policies.Identity,
                        policy => policy
                            .AddAuthenticationSchemes(Schemes.Application)
                            .AddRequirements(new UserRequirement()));
                })
                .AddScoped<IAuthorizationHandler, UserHandler>()
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = Schemes.Application;
                    options.DefaultChallengeScheme = Schemes.Application;
                    options.DefaultSignInScheme = Schemes.External;
                })
                .AddCookie(Schemes.Application, options =>
                {
                    options.Cookie.Name = Schemes.Application;
                    options.LoginPath = new("/Identity/Account/Login");
                    options.LogoutPath = new("/Identity/Account/Logout");
                    options.AccessDeniedPath = new("/Identity/Account/AccessDenied");
                    options.Events = new()
                    {
                        OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync,
                    };
                })
                .AddCookie(Schemes.External, options =>
                {
                    options.Cookie.Name = Schemes.External;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                })
                .AddCookie(Schemes.TwoFactorRememberMe, options =>
                {
                    options.LoginPath = new("/Identity/Account/Login");
                    options.LogoutPath = new("/Identity/Account/Logout");
                    options.AccessDeniedPath = new("/Identity/Account/AccessDenied");
                    options.Cookie.Name = Schemes.TwoFactorRememberMe;
                    options.Events = new()
                    {
                        OnValidatePrincipal = SecurityStampValidator.ValidateAsync<ITwoFactorSecurityStampValidator>,
                    };
                })
                .AddCookie(Schemes.TwoFactorUserId, options =>
                {
                    options.Cookie.Name = Schemes.TwoFactorUserId;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                });

        if (jwtOptionsDefined)
        {
            services
                .AddOptions<JwtOptions>()
                .BindConfiguration(JwtOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            authenticationBuilder.AddJwtBearer(options =>
            {
                options.ClaimsIssuer = jwtOptions!.ValidIssuer;
                options.Audience = jwtOptions.ValidAudience;
                options.SaveToken = true;
                options.TokenValidationParameters = new()
                {
                    ValidateIssuerSigningKey = true,
                    ValidAudience = jwtOptions.ValidAudience,
                    ValidIssuer = jwtOptions.ValidIssuer,
                    IssuerSigningKey = jwtOptions.SecurityKey,
                    ClockSkew = TimeSpan.Zero,
                };
            });
        }
        else
        {
            authenticationBuilder.AddJwtBearer();
        }

        foreach (var providerName in oidcProviderNames)
        {
            var providerOptions = oidcProviderSection.GetValid<OidcProviderOptions>(providerName);
            issuers.Add(providerOptions.ServerRealm.AbsoluteUri, providerName);
            authenticationBuilder.AddJwtBearer(providerName, providerName, options =>
            {
                options.Authority = providerOptions.ServerRealm.AbsoluteUri;
                options.MetadataAddress = providerOptions.Metadata.AbsoluteUri;
                options.Audience = providerOptions.ClientId;
                options.RequireHttpsMetadata = providerOptions.RequireHttpsMetadata;
                options.SaveToken = true;

                options.TokenValidationParameters = new()
                {
                    ValidateIssuerSigningKey = true,
                    ValidAudience = providerOptions.ClientId,
                    ClockSkew = TimeSpan.Zero,
                    AuthenticationType = providerName,
                };
            });

            var displayName = providerOptions.DisplayName ?? providerName;
            authenticationBuilder.AddOpenIdConnect(providerName + OidcSuffix, displayName, options =>
            {
                options.SignInScheme = Schemes.External;
                options.Authority = providerOptions.ServerRealm.AbsoluteUri;
                options.ClientId = providerOptions.ClientId;
                options.ClientSecret = providerOptions.ClientSecret;
                options.MetadataAddress = providerOptions.Metadata.AbsoluteUri;
                options.RequireHttpsMetadata = providerOptions.RequireHttpsMetadata;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.SaveTokens = true;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
                options.TokenValidationParameters = new()
                {
                    NameClaimType = "name",
                    RoleClaimType = ClaimTypes.Role,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = providerOptions.ClientId,
                    ClockSkew = TimeSpan.Zero,
                    AuthenticationType = providerName,
                };
            });
        }

        services.AddSingleton<IAuthenticationSchemeProvider, CustomAuthenticationSchemeProvider>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AuthenticationOptions>>();
            var accessor = provider.GetRequiredService<IHttpContextAccessor>();
            var tokenHandler = provider.GetRequiredService<JwtSecurityTokenHandler>();
            return new(options, accessor, tokenHandler, issuers);
        });

        return services;
    }
}
