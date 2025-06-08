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

public sealed class ClaimRepository : IRepository<ClaimEntity>
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
    public ValueTask<ClaimEntity> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<ClaimEntity> GetAsync(Guid id, Guid userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<ClaimEntity?> FindAsync(Guid id, Guid userId, Access access, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<ClaimEntity?> FindAsync(Guid id, Guid userId, Access access, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<ClaimEntity?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<ClaimEntity?> FindAsync(Guid id, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask<List<ClaimEntity>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            $"""
             SELECT id AS Id,
                    created_at AS CreatedAt,
                    created_by_user_id AS CreatedByUserId,
                    user_id AS UserId,
                    claim_type AS ClaimType,
                    claim_value AS ClaimValue
             FROM identity.claims
             WHERE claims.user_id = @userId;
             """,
            new { userId },
            cancellationToken: cancellationToken);

        var enumerable = await _connection.QueryAsync<ClaimEntity>(command);
        return enumerable as List<ClaimEntity> ?? enumerable.ToList();
    }

    /// <inheritdoc />
    public ValueTask<List<ClaimEntity>> GetAsync(Guid userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ClaimEntity> GetUnbufferedAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ClaimEntity> GetUnbufferedAsync(Guid userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<int> DeleteAsync(Guid id, Guid userId, NpgsqlTransaction transaction)
    {
        throw new NotImplementedException();
    }
}
