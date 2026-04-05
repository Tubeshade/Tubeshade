using System;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Tubeshade.Data.AccessControl;

public sealed class OwnershipRepository
{
    private readonly NpgsqlConnection _connection;

    public OwnershipRepository(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public async ValueTask<Guid> AddDefaultForUserAsync(Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
             INSERT
             INTO identity.ownerships (created_by_user_id, modified_by_user_id, owner_id, user_id, access)
             SELECT @{nameof(userId)}, @{nameof(userId)}, @{nameof(userId)}, system.id, '{Access.Names.Owner}'
             FROM system;

             WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
             INSERT
             INTO identity.ownerships (created_by_user_id, modified_by_user_id, owner_id, user_id, access)
             SELECT @{nameof(userId)}, @{nameof(userId)}, system.id, @{nameof(userId)}, '{Access.Names.Read}' 
             FROM system;

             WITH users AS (SELECT id FROM identity.users WHERE id = @{nameof(userId)})
             INSERT
             INTO identity.ownerships (id, created_by_user_id, modified_by_user_id, owner_id, user_id, access)
             SELECT id, id, id, id, id, '{Access.Names.Owner}' 
             FROM users
             RETURNING id;
             """,
            new { userId },
            transaction);

        return await _connection.QuerySingleAsync<Guid>(command);
    }
}
