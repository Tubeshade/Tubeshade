using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Abstractions;

public interface INamedRepository<TEntity> : IRepository<TEntity>
    where TEntity : INamedEntity
{
    ValueTask<TEntity?> FindAsync(
        string name,
        long userId,
        Permissions permissions,
        CancellationToken cancellationToken = default);

    ValueTask<TEntity?> FindAsync(string name, long userId, Permissions permissions, NpgsqlTransaction transaction);

    ValueTask<TEntity?> FindAsync(string name, CancellationToken cancellationToken = default);

    ValueTask<TEntity?> FindAsync(string name, NpgsqlTransaction transaction);
}
