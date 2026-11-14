using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Tubeshade.Data.Identity;

public sealed partial class UserLoginRepository
{
    private readonly NpgsqlConnection _connection;

    public UserLoginRepository(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public async ValueTask<int> AddAsync(UserLogin userLogin, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(Queries.Insert, userLogin, transaction);
        return await _connection.ExecuteAsync(command);
    }

    public async ValueTask<List<UserLogin>> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(Queries.Get, new { userId }, cancellationToken: cancellationToken);
        var enumerable = await _connection.QueryAsync<UserLogin>(command);
        return enumerable as List<UserLogin> ?? enumerable.ToList();
    }

    public async ValueTask<int> UpdateAsync(
        string loginProvider,
        string providerKey,
        string refreshToken,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(Queries.UpdateRefreshToken, new { loginProvider, providerKey, refreshToken }, transaction);
        return await _connection.ExecuteAsync(command);
    }

    public async ValueTask<int> DeleteAsync(UserLogin userLogin, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(Queries.Delete, userLogin, transaction);
        return await _connection.ExecuteAsync(command);
    }
}
