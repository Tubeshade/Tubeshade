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

    ValueTask<TEntity> GetAsync(Guid id, long userId, CancellationToken cancellationToken = default);

    ValueTask<TEntity> GetAsync(Guid id, long userId, NpgsqlTransaction transaction);

    /// <summary>Finds the entity if the user holds Read and <paramref name="permissions"/> on its owner.</summary>
    ValueTask<TEntity?> FindAsync(Guid id, long userId, Permissions permissions, CancellationToken cancellationToken = default);

    ValueTask<TEntity?> FindAsync(Guid id, long userId, Permissions permissions, NpgsqlTransaction transaction);

    ValueTask<TEntity?> FindForSystemAsync(SystemPrincipal system, Guid id, NpgsqlTransaction transaction);

    ValueTask<List<TEntity>> GetAsync(long userId, CancellationToken cancellationToken = default);

    ValueTask<List<TEntity>> GetAsync(long userId, NpgsqlTransaction transaction);

    ValueTask<List<TEntity>> GetForSystemAsync(SystemPrincipal system, NpgsqlTransaction transaction);

    IAsyncEnumerable<TEntity> GetUnbufferedAsync(long userId);

    IAsyncEnumerable<TEntity> GetUnbufferedAsync(long userId, NpgsqlTransaction transaction);

    ValueTask<int> DeleteAsync(Guid id, long userId, NpgsqlTransaction transaction);
}
