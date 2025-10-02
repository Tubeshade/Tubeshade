using System;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Tubeshade.Data.AccessControl;

public sealed class OwnershipRepository
{
    private readonly NpgsqlConnection _connection;

    private static string InsertSql =>
        // lang=sql
        $"""
         WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
         INSERT
         INTO identity.ownerships (created_by_user_id, modified_by_user_id, owner_id, user_id, access)
         SELECT @userId, @userId, system.id, @userId, '{Access.Names.Owner}' 
         FROM system
         RETURNING id;

         WITH users AS (SELECT id FROM identity.users WHERE id = @userId)
         INSERT
         INTO identity.ownerships (id, created_by_user_id, modified_by_user_id, owner_id, user_id, access)
         SELECT id, id, id, id, id, '{Access.Names.Owner}' 
         FROM users
         RETURNING id;
         """;

    public OwnershipRepository(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public async ValueTask<Guid> AddDefaultForUserAsync(Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(InsertSql, new { userId }, transaction);
        return await _connection.QuerySingleAsync<Guid>(command);
    }
}
