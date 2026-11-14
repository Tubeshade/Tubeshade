using System;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Tubeshade.Data.AccessControl;

public sealed partial class OwnerRepository
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
            Queries.AddPersonal,
            new { userId, PersonalPermissions = Permissions.All, SystemPermissions = Permissions.Read },
            transaction);

        return await _connection.QuerySingleAsync<Guid>(command);
    }

    public async ValueTask<Guid> GetPersonalOwnerIdAsync(Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            Queries.GetPersonalOwnerId,
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
            Queries.GetPersonalUserIdForSystem,
            new { ownerId },
            transaction);

        return await _connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }
}
