using System;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Tubeshade.Data.AccessControl;

public sealed class OwnerRepository
{
    private readonly NpgsqlConnection _connection;

    public OwnerRepository(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Creates the user's personal owner, gives the user all permissions on it,
    /// and Read on the system owner, which holds work that belongs to no user.
    /// </summary>
    public async ValueTask<Guid> AddPersonalAsync(Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             WITH new_owner AS
                 (INSERT INTO access.owners (created_by_user_id, modified_by_user_id, name, personal_for)
                  SELECT users.id, users.id, users.name, users.id
                  FROM identity.users
                  WHERE users.id = @{nameof(userId)}
                  RETURNING id),

                 own_grant AS
                 (INSERT INTO access.grants (principal_id, owner_id, permissions, granted_by)
                  SELECT @{nameof(userId)}, new_owner.id, {(int)Permissions.All}, @{nameof(userId)}
                  FROM new_owner),

                 system_grant AS
                 (INSERT INTO access.grants (principal_id, owner_id, permissions, granted_by)
                  SELECT @{nameof(userId)}, owners.id, {(int)Permissions.Read}, @{nameof(userId)}
                  FROM access.owners
                      INNER JOIN identity.users ON owners.personal_for = users.id
                  WHERE users.normalized_name = 'SYSTEM')

             SELECT id FROM new_owner;
             """,
            new { userId },
            transaction);

        return await _connection.QuerySingleAsync<Guid>(command);
    }

    public async ValueTask<Guid> GetPersonalOwnerIdAsync(Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            $"SELECT id FROM access.owners WHERE personal_for = @{nameof(userId)};",
            new { userId },
            transaction);

        return await _connection.QuerySingleAsync<Guid>(command);
    }

    public async ValueTask<Guid?> GetPersonalUserIdForSystemAsync(
        SystemPrincipal system,
        Guid ownerId,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            $"SELECT personal_for FROM access.owners WHERE id = @{nameof(ownerId)};",
            new { ownerId },
            transaction);

        return await _connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }
}
