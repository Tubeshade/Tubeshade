using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Identity;

public sealed partial class ClaimRepository : IRepository<ClaimEntity>
{
    private readonly NpgsqlConnection _connection;

    public ClaimRepository(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public ValueTask<Guid?> AddAsync(ClaimEntity entity, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<ClaimEntity> GetAsync(Guid id, long userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<ClaimEntity> GetAsync(Guid id, long userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<ClaimEntity?> FindAsync(Guid id, long userId, Permissions permissions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<ClaimEntity?> FindAsync(Guid id, long userId, Permissions permissions, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<ClaimEntity?> FindForSystemAsync(SystemPrincipal system, Guid id, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<List<ClaimEntity>> GetForSystemAsync(SystemPrincipal system, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask<List<ClaimEntity>> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.Get,
            new { userId },
            cancellationToken: cancellationToken);

        var enumerable = await _connection.QueryAsync<ClaimEntity>(command);
        return enumerable as List<ClaimEntity> ?? enumerable.ToList();
    }

    /// <inheritdoc />
    public ValueTask<List<ClaimEntity>> GetAsync(long userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ClaimEntity> GetUnbufferedAsync(long userId)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ClaimEntity> GetUnbufferedAsync(long userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<int> DeleteAsync(Guid id, long userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }
}
