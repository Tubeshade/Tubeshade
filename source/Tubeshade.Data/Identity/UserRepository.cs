using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Identity;

public sealed class UserRepository : IModifiableRepository<UserEntity>, INamedRepository<UserEntity>
{
    private readonly NpgsqlConnection _connection;

    private static string SelectSql =>
        $"""
         SELECT users.id AS {nameof(UserEntity.Id)},
                users.created_at AS {nameof(UserEntity.CreatedAt)},
                users.created_by_user_id AS {nameof(UserEntity.CreatedByUserId)},
                users.modified_at AS {nameof(UserEntity.ModifiedAt)},
                users.modified_by_user_id AS {nameof(UserEntity.ModifiedByUserId)},
                users.name AS {nameof(UserEntity.Name)},
                users.normalized_name AS {nameof(UserEntity.NormalizedName)},
                users.full_name AS {nameof(UserEntity.FullName)},
                users.email AS {nameof(UserEntity.Email)},
                users.normalized_email AS {nameof(UserEntity.NormalizedEmail)},
                users.email_confirmed AS {nameof(UserEntity.EmailConfirmed)},
                users.password_hash AS {nameof(UserEntity.PasswordHash)},
                users.security_stamp AS {nameof(UserEntity.SecurityStamp)},
                users.concurrency_stamp AS {nameof(UserEntity.ConcurrencyStamp)},
                users.two_factor_enabled AS {nameof(UserEntity.TwoFactorEnabled)},
                users.lockout_end AS {nameof(UserEntity.LockoutEnd)},
                users.lockout_enabled AS {nameof(UserEntity.LockoutEnabled)},
                users.access_failed_count AS {nameof(UserEntity.AccessFailedCount)}
         FROM identity.users
         """;

    private static string InsertSql =>
        //lang=sql
        $"""
         WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
         INSERT
         INTO identity.users (
                     id,
                     created_by_user_id,
                     modified_by_user_id,
                     name,
                     normalized_name,
                     full_name,
                     email,
                     normalized_email,
                     email_confirmed,
                     password_hash,
                     security_stamp,
                     concurrency_stamp,
                     two_factor_enabled,
                     lockout_end,
                     lockout_enabled,
                     access_failed_count)
         SELECT @Id,
                system.id,
                system.id,
                @{nameof(UserEntity.Name)},
                @{nameof(UserEntity.NormalizedName)},
                @{nameof(UserEntity.FullName)},
                @{nameof(UserEntity.Email)},
                @{nameof(UserEntity.NormalizedEmail)},
                @{nameof(UserEntity.EmailConfirmed)},
                @{nameof(UserEntity.PasswordHash)},
                @{nameof(UserEntity.SecurityStamp)},
                @{nameof(UserEntity.ConcurrencyStamp)},
                @{nameof(UserEntity.TwoFactorEnabled)},
                @{nameof(UserEntity.LockoutEnd)},
                @{nameof(UserEntity.LockoutEnabled)},
                @{nameof(UserEntity.AccessFailedCount)}
         FROM system
         RETURNING id;
         """;

    private static string FindByNameSql =>
        //lang=sql
        $"""
         {SelectSql}
         WHERE users.normalized_name = @name;
         """;

    public UserRepository(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public async ValueTask<Guid?> AddAsync(UserEntity entity, NpgsqlTransaction transaction)
    {
        entity.NormalizeInvariant();
        await _connection.OpenConnection();
        var command = new CommandDefinition(InsertSql, entity, transaction);
        return await _connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }

    /// <inheritdoc />
    public async ValueTask<UserEntity> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            $"""
             {SelectSql}
             WHERE users.id = @id;
             """,
            new { id, userId },
            cancellationToken: cancellationToken);

        return await _connection.QuerySingleAsync<UserEntity>(command);
    }

    /// <inheritdoc />
    public ValueTask<UserEntity> GetAsync(Guid id, Guid userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(Guid id, Guid userId, Access access,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(Guid id, Guid userId, Access access, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask<UserEntity?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            $"""
             {SelectSql}
             WHERE users.id = @id;
             """,
            new { id },
            cancellationToken: cancellationToken);

        return await _connection.QuerySingleOrDefaultAsync<UserEntity>(command);
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(Guid id, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<UserEntity?> FindByLoginAsync(
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            $"""
             {SelectSql}
             INNER JOIN identity.user_logins ON users.id = user_logins.user_id
             WHERE provider_key = @{nameof(providerKey)}
               AND login_provider = @{nameof(loginProvider)};
             """,
            new { loginProvider, providerKey },
            cancellationToken: cancellationToken);

        return await _connection.QuerySingleOrDefaultAsync<UserEntity>(command);
    }

    /// <inheritdoc />
    public ValueTask<List<UserEntity>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<List<UserEntity>> GetAsync(Guid userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<UserEntity> GetUnbufferedAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<UserEntity> GetUnbufferedAsync(Guid userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask<int> UpdateAsync(UserEntity entity, NpgsqlTransaction transaction)
    {
        entity.NormalizeInvariant();
        var command = new CommandDefinition(
            //lang=sql
            $"""
             UPDATE identity.users
             SET modified_at         = CURRENT_TIMESTAMP,
                 modified_by_user_id = @Id,
                 name                = @Name,
                 normalized_name     = @NormalizedName,
                 full_name = @FullName,
                 email               = @Email,
                 normalized_email    = @NormalizedEmail,
                 email_confirmed     = @EmailConfirmed,
                 password_hash       = @PasswordHash,
                 security_stamp      = @SecurityStamp,
                 concurrency_stamp   = @ConcurrencyStamp,
                 two_factor_enabled  = @TwoFactorEnabled,
                 lockout_end         = @LockoutEnd,
                 lockout_enabled     = @LockoutEnabled,
                 access_failed_count = @AccessFailedCount
             WHERE users.id = @Id;
             """,
            entity,
            transaction);

        return await _connection.ExecuteAsync(command);
    }

    public ValueTask<int> DeleteAsync(UserEntity user, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<int> DeleteAsync(Guid id, Guid userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(string name, Guid userId, Access access,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(string name, Guid userId, Access access, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask<UserEntity?> FindAsync(string name, CancellationToken cancellationToken = default)
    {
        name = name.NormalizeInvariant();
        await _connection.OpenConnection(cancellationToken);
        var command = new CommandDefinition(FindByNameSql, new { name }, cancellationToken: cancellationToken);
        return await _connection.QuerySingleOrDefaultAsync<UserEntity>(command);
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(string name, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<UserEntity?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        email = email.NormalizeInvariant(false);
        await _connection.OpenConnection(cancellationToken);
        var command = new CommandDefinition(
            $"""
             {SelectSql}
             WHERE users.normalized_email = @email;
             """,
            new { email },
            cancellationToken: cancellationToken);

        return await _connection.QuerySingleOrDefaultAsync<UserEntity>(command);
    }
}
