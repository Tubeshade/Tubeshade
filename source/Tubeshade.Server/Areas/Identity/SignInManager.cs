using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tubeshade.Data.Identity;

namespace Tubeshade.Server.Areas.Identity;

public sealed class SignInManager : SignInManager<UserEntity>
{
    public SignInManager(
        UserManager<UserEntity> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<UserEntity> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<UserEntity> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<AuthenticationScheme>> GetExternalAuthenticationSchemesAsync()
    {
        var schemes = await base.GetExternalAuthenticationSchemesAsync();
        return schemes.Where(scheme => scheme.HandlerType == typeof(OpenIdConnectHandler));
    }
}
