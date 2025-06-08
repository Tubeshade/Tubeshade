using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tubeshade.Data.Identity;

namespace Tubeshade.Server.Areas.Identity;

internal sealed class SignInManager : SignInManager<UserEntity>
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
    public override async Task<SignInResult> PasswordSignInAsync(
        string userName,
        string password,
        bool isPersistent,
        bool lockoutOnFailure)
    {
        var user = await UserManager.FindByNameAsync(userName) ??
                   await UserManager.FindByEmailAsync(userName);

        if (user is null)
        {
            return SignInResult.Failed;
        }

        return await PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
    }
}
