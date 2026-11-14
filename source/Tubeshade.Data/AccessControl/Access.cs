using Dapper;

namespace Tubeshade.Data.AccessControl;

public static class Access
{
    public static DynamicParameters WithPermissions(object values, Permissions permissions)
    {
        var parameters = new DynamicParameters(values);
        parameters.Add(nameof(IAccessParameters.Permissions), (int)permissions);
        return parameters;
    }

    public static DynamicParameters? Parameters(
        Principal principal,
        Resource resource,
        Permissions permissions,
        object? values = null)
    {
        if (!principal.Allows(resource, permissions))
        {
            return null;
        }

        var parameters = new DynamicParameters(values);
        parameters.Add(nameof(IAccessParameters.PrincipalId), principal.GrantHolderId);
        parameters.Add(nameof(IAccessParameters.Permissions), (int)permissions);
        return parameters;
    }
}
