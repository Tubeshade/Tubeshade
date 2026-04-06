using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Tubeshade.Data.Identity;

public sealed class UserLoginRepository
{
    private readonly NpgsqlConnection _connection;

    public UserLoginRepository(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public async ValueTask<int> AddAsync(UserLogin userLogin, NpgsqlTransaction transaction)
    {
        const string sql =
            $"""
             INSERT INTO identity.user_logins 
                 (provider_key,
                  provider_display_name,
                  user_id,
                  login_provider,
                  refresh_token)

             VALUES (@{nameof(UserLogin.ProviderKey)},
                     @{nameof(UserLogin.ProviderDisplayName)},
                     @{nameof(UserLogin.UserId)},
                     @{nameof(UserLogin.LoginProvider)},
                     @{nameof(UserLogin.RefreshToken)});
             """;

        var command = new CommandDefinition(sql, userLogin, transaction);
        return await _connection.ExecuteAsync(command);
    }

    public async ValueTask<List<UserLogin>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            SELECT provider_key, provider_display_name, user_id, login_provider, refresh_token
            FROM identity.user_logins
            WHERE user_id = @userId;
            """;

        var command = new CommandDefinition(sql, new { userId }, cancellationToken: cancellationToken);
        var enumerable = await _connection.QueryAsync<UserLogin>(command);
        return enumerable as List<UserLogin> ?? enumerable.ToList();
    }

    public async ValueTask<int> UpdateAsync(
        string loginProvider,
        string providerKey,
        string refreshToken,
        NpgsqlTransaction transaction)
    {
        const string sql =
            $"""
             UPDATE identity.user_logins
             SET refresh_token = @{nameof(refreshToken)}
             WHERE provider_key = @{nameof(providerKey)} AND login_provider = @{nameof(loginProvider)};
             """;

        var command = new CommandDefinition(sql, new { loginProvider, providerKey, refreshToken }, transaction);
        return await _connection.ExecuteAsync(command);
    }

    public async ValueTask<int> DeleteAsync(UserLogin userLogin, NpgsqlTransaction transaction)
    {
        const string sql =
            $"""
             DELETE FROM identity.user_logins
             WHERE user_id = @{nameof(UserLogin.UserId)} AND provider_key = @{nameof(UserLogin.ProviderKey)};
             """;

        var command = new CommandDefinition(sql, userLogin, transaction);
        return await _connection.ExecuteAsync(command);
    }
}
