using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Identity;

public sealed partial class UserRepository : IModifiableRepository<UserEntity>, INamedRepository<UserEntity>
{
    private readonly NpgsqlConnection _connection;

    public UserRepository(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    /// <remarks>Sets <see cref="UserEntity.Id"/> to the id of the new user.</remarks>
    public async ValueTask<Guid?> AddAsync(UserEntity entity, NpgsqlTransaction transaction)
    {
        entity.NormalizeInvariant();
        await _connection.OpenConnection();
        var command = new CommandDefinition(Queries.Insert, entity, transaction);
        if (await _connection.QuerySingleOrDefaultAsync<long?>(command) is not { } id)
        {
            return null;
        }

        entity.Id = id;
        return entity.PublicId;
    }

    /// <inheritdoc />
    public async ValueTask<UserEntity> GetAsync(Guid id, long userId, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.FindById,
            new { id, userId },
            cancellationToken: cancellationToken);

        return await _connection.QuerySingleAsync<UserEntity>(command);
    }

    /// <inheritdoc />
    public ValueTask<UserEntity> GetAsync(Guid id, long userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(Guid id, long userId, Permissions permissions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(Guid id, long userId, Permissions permissions, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <summary>Finds the user by id, for authentication; users are not owned data.</summary>
    public async ValueTask<UserEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.FindById,
            new { id },
            cancellationToken: cancellationToken);

        return await _connection.QuerySingleOrDefaultAsync<UserEntity>(command);
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindForSystemAsync(SystemPrincipal system, Guid id, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    public ValueTask<List<UserEntity>> GetForSystemAsync(SystemPrincipal system, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<UserEntity?> FindByLoginAsync(
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.FindByLogin,
            new { loginProvider, providerKey },
            cancellationToken: cancellationToken);

        return await _connection.QuerySingleOrDefaultAsync<UserEntity>(command);
    }

    /// <inheritdoc />
    public ValueTask<List<UserEntity>> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<List<UserEntity>> GetAsync(long userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<UserEntity> GetUnbufferedAsync(long userId)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<UserEntity> GetUnbufferedAsync(long userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask<int> UpdateAsync(UserEntity entity, NpgsqlTransaction transaction)
    {
        entity.NormalizeInvariant();
        var command = new CommandDefinition(
            Queries.Update,
            entity,
            transaction);

        return await _connection.ExecuteAsync(command);
    }

    public ValueTask<int> DeleteAsync(UserEntity user, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<int> DeleteAsync(Guid id, long userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(string name, long userId, Permissions permissions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(string name, long userId, Permissions permissions, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask<UserEntity?> FindAsync(string name, CancellationToken cancellationToken = default)
    {
        name = name.NormalizeInvariant();
        await _connection.OpenConnection(cancellationToken);
        var command = new CommandDefinition(Queries.FindByName, new { name }, cancellationToken: cancellationToken);
        return await _connection.QuerySingleOrDefaultAsync<UserEntity>(command);
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(string name, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }
}
