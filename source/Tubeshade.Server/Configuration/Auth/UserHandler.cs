using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Tubeshade.Data.Identity;

namespace Tubeshade.Server.Configuration.Auth;

/// <summary>Authorization handler which handles the <see cref="UserRequirement"/>.</summary>
public sealed class UserHandler : AuthorizationHandler<UserRequirement>
{
    private readonly ILogger<UserHandler> _logger;
    private readonly UserRepository _userRepository;

    /// <summary>Initializes a new instance of the <see cref="UserHandler"/> class.</summary>
    /// <param name="logger"></param>
    /// <param name="userRepository">The repository for performing CRUD operations on <see cref="UserEntity"/>.</param>
    public UserHandler(ILogger<UserHandler> logger, UserRepository userRepository)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserRequirement requirement)
    {
        if (context.User.GetLoginProvider() is { } loginProvider)
        {
            _logger.LogDebug("Trying to find user by login provider key");

            var claims = context.User.FindAll(ClaimTypes.NameIdentifier).DistinctBy(claim => claim.Value).ToArray();
            if (claims is [var nameClaim])
            {
                var userByLogin = await _userRepository.FindByLoginAsync(loginProvider, nameClaim.Value);
                if (userByLogin is not null)
                {
                    context.User.AddUserId(userByLogin.Id);
                    context.Succeed(requirement);
                    return;
                }
            }
            else
            {
                _logger.LogDebug("Claims principal does not have a name identifier claim");
            }
        }

        _logger.LogDebug("Trying to find user by id");

        if (!context.User.TryGetUserId(out var id))
        {
            context.Fail(new(this, "User does not have valid id claim"));
            return;
        }

        var user = await _userRepository.FindAsync(id);
        if (user is not null)
        {
            context.User.AddUserId(user.Id);
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(new(this, "User is not an application user"));
        }
    }
}
