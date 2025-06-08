using Microsoft.AspNetCore.Authorization;
using Tubeshade.Data.Identity;

namespace Tubeshade.Server.Configuration.Auth;

/// <summary>An <see cref="IAuthorizationRequirement"/> that indicates that an <see cref="UserEntity"/> is required.</summary>
public sealed class UserRequirement : IAuthorizationRequirement;
