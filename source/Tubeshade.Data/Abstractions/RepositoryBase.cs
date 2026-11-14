using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Abstractions;

public abstract class RepositoryBase<TEntity> : IRepository<TEntity>
    where TEntity : IEntity
{
    // lang=sql
    public const string AccessibleLibraries =
        $"SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN ({Access.AccessibleOwners})";

    protected RepositoryBase(NpgsqlConnection connection)
    {
        Connection = connection;
    }

    protected NpgsqlConnection Connection { get; }

    protected abstract string TableName { get; }

    protected abstract string InsertSql { get; }

    protected abstract string SelectSql { get; }

    protected virtual string AccessFilter =>
        $"""
         ({TableName}.owner_id IN ({Access.AccessibleOwners}))
         """;

    protected virtual string SelectSingleSql =>
        $"""
         {SelectSql}
         WHERE {TableName}.id = @{nameof(GetSingleParameters.Id)};
         """;

    protected virtual string SelectAccessibleSql =>
        $"""
         {SelectSql}
         WHERE {AccessFilter}
         """;

    protected virtual string SelectAccessibleSingleSql =>
        $"""
         {SelectAccessibleSql}
           AND {TableName}.id = @{nameof(GetSingleParameters.Id)};
         """;

    protected virtual string DeleteSql =>
        $"""
         DELETE FROM {TableName}
         WHERE {AccessFilter}
           AND {TableName}.id = @{nameof(GetSingleParameters.Id)};
         """;

    protected static string CanCreateIn(string ownerIdParameter) =>
        $"""
         @{ownerIdParameter} IN (SELECT owner_id FROM access.accessible_owners(@{nameof(IEntity.CreatedByUserId)}, {(int)Permissions.Create}))
         """;

    /// <inheritdoc />
    public async ValueTask<Guid?> AddAsync(TEntity entity, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(InsertSql, entity, transaction);
        return await Connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }

    /// <inheritdoc />
    public async ValueTask<TEntity> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            SelectAccessibleSingleSql,
            new GetSingleParameters(id, userId, Permissions.Read),
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleAsync<TEntity>(command);
    }

    /// <inheritdoc />
    public async ValueTask<TEntity> GetAsync(Guid id, Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            SelectAccessibleSingleSql,
            new GetSingleParameters(id, userId, Permissions.Read),
            transaction);

        return await Connection.QuerySingleAsync<TEntity>(command);
    }

    /// <inheritdoc />
    public async ValueTask<TEntity?> FindAsync(
        Guid id,
        Guid userId,
        Permissions permissions,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            SelectAccessibleSingleSql,
            new GetSingleParameters(id, userId, permissions | Permissions.Read),
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleOrDefaultAsync<TEntity>(command);
    }

    /// <inheritdoc />
    public async ValueTask<TEntity?> FindAsync(Guid id, Guid userId, Permissions permissions, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            SelectAccessibleSingleSql,
            new GetSingleParameters(id, userId, permissions | Permissions.Read),
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<TEntity>(command);
    }

    /// <inheritdoc />
    public async ValueTask<TEntity?> FindForSystemAsync(SystemPrincipal system, Guid id, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(SelectSingleSql, new { Id = id }, transaction);
        return await Connection.QuerySingleOrDefaultAsync<TEntity>(command);
    }

    /// <inheritdoc />
    public async ValueTask<List<TEntity>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            SelectAccessibleSql,
            new GetParameters(userId, Permissions.Read),
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<TEntity>(command);
        return enumerable as List<TEntity> ?? enumerable.ToList();
    }

    /// <inheritdoc />
    public async ValueTask<List<TEntity>> GetAsync(Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            SelectAccessibleSql,
            new GetParameters(userId, Permissions.Read),
            transaction);

        var enumerable = await Connection.QueryAsync<TEntity>(command);
        return enumerable as List<TEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<TEntity>> GetForSystemAsync(SystemPrincipal system, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(SelectSql, null, transaction);
        var enumerable = await Connection.QueryAsync<TEntity>(command);
        return enumerable as List<TEntity> ?? enumerable.ToList();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TEntity> GetUnbufferedAsync(Guid userId)
    {
        return Connection.QueryUnbufferedAsync<TEntity>(
            SelectAccessibleSql,
            new GetParameters(userId, Permissions.Read));
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TEntity> GetUnbufferedAsync(Guid userId, NpgsqlTransaction transaction)
    {
        return Connection.QueryUnbufferedAsync<TEntity>(
            SelectAccessibleSql,
            new GetParameters(userId, Permissions.Read),
            transaction);
    }

    /// <inheritdoc />
    public async ValueTask<int> DeleteAsync(Guid id, Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(DeleteSql, new DeleteParameters(id, userId), transaction);
        return await Connection.ExecuteAsync(command);
    }
}
