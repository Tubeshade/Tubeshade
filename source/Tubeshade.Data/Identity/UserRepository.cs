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

    /// <inheritdoc />
    public async ValueTask<Guid?> AddAsync(UserEntity entity, NpgsqlTransaction transaction)
    {
        entity.NormalizeInvariant();
        await _connection.OpenConnection();
        var command = new CommandDefinition(Queries.Insert, entity, transaction);
        return await _connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }

    /// <inheritdoc />
    public async ValueTask<UserEntity> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.FindById,
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
    public ValueTask<UserEntity?> FindAsync(Guid id, Guid userId, Permissions permissions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(Guid id, Guid userId, Permissions permissions, NpgsqlTransaction transaction)
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
    public ValueTask<int> DeleteAsync(Guid id, Guid userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(string name, Guid userId, Permissions permissions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<UserEntity?> FindAsync(string name, Guid userId, Permissions permissions, NpgsqlTransaction transaction)
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
