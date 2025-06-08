using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Tubeshade.Server.Configuration.Auth;

/// <summary>Requires the user to have the <see cref="Roles.Administrator"/> role.</summary>
public sealed class AdministratorRoleRequirement() : RolesAuthorizationRequirement([Roles.Administrator]);
