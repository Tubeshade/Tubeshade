using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Abstractions;

public interface IRepository<TEntity>
{
    ValueTask<Guid?> AddAsync(TEntity entity, NpgsqlTransaction transaction);

    ValueTask<TEntity> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    ValueTask<TEntity> GetAsync(Guid id, Guid userId, NpgsqlTransaction transaction);

    ValueTask<TEntity?> FindAsync(Guid id, Guid userId, Access access, CancellationToken cancellationToken = default);

    ValueTask<TEntity?> FindAsync(Guid id, Guid userId, Access access, NpgsqlTransaction transaction);

    ValueTask<TEntity?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    ValueTask<TEntity?> FindAsync(Guid id, NpgsqlTransaction transaction);

    ValueTask<List<TEntity>> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    ValueTask<List<TEntity>> GetAsync(Guid userId, NpgsqlTransaction transaction);

    IAsyncEnumerable<TEntity> GetUnbufferedAsync(Guid userId);

    IAsyncEnumerable<TEntity> GetUnbufferedAsync(Guid userId, NpgsqlTransaction transaction);

    ValueTask<int> DeleteAsync(Guid id, Guid userId, NpgsqlTransaction transaction);
}
