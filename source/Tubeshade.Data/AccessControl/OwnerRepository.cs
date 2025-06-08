using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Tubeshade.Data.AccessControl;

public sealed class OwnerRepository
{
    private readonly DbConnection _connection;

    private static string InsertSql =>
        //lang=sql
        """
        WITH users AS (SELECT id, name FROM identity.users WHERE id = @userId)
        INSERT INTO identity.owners (id, created_by_user_id, modified_by_user_id, name)
        SELECT id, id, id, name
        FROM users
        RETURNING id;
        """;

    public OwnerRepository(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public async ValueTask<Guid> AddDefaultForUserAsync(Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(InsertSql, new { userId }, transaction);
        return await _connection.QuerySingleAsync<Guid>(command);
    }
}
