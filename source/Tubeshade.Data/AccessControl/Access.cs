using Dapper;

namespace Tubeshade.Data.AccessControl;

public static class Access
{
    // lang=sql
    public const string AccessibleOwners =
        $"SELECT owner_id FROM access.accessible_owners(@{nameof(IAccessParameters.PrincipalId)}, @{nameof(IAccessParameters.Permissions)})";

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
