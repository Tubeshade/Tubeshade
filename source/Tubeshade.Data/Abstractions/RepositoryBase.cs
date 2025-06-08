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
    protected RepositoryBase(NpgsqlConnection connection)
    {
        Connection = connection;
    }

    protected NpgsqlConnection Connection { get; }

    protected abstract string TableName { get; }

    protected virtual string InsertAccessFilter =>
        $"""
         EXISTS(SELECT owners.id
                FROM identity.owners
                         INNER JOIN identity.ownerships ON ownerships.owner_id = owners.id AND
                                                  ownerships.user_id = @{nameof(IEntity.CreatedByUserId)} AND
                                                  (ownerships.access = 'append' OR ownerships.access = 'owner'))
         """;

    protected abstract string InsertSql { get; }

    protected virtual string AccessCte =>
        $"""
         WITH accessible AS
         (SELECT {TableName}.id
          FROM {TableName}
                   INNER JOIN identity.owners ON owners.id = {TableName}.owner_id
                   INNER JOIN identity.ownerships ON
              ownerships.owner_id = owners.id AND
              ownerships.user_id = @{nameof(GetParameters.UserId)} AND
              (ownerships.access = @{nameof(GetParameters.Access)} OR ownerships.access = 'owner'))
         """;

    protected abstract string SelectSql { get; }

    protected virtual string AccessFilter =>
        $"""
         ({TableName}.id IN (SELECT id FROM accessible))
         """;

    protected virtual string SelectSingleSql =>
        $"""
         {SelectSql}
         WHERE {TableName}.id = @{nameof(GetSingleParameters.Id)};
         """;

    protected virtual string SelectAccessibleSql =>
        $"""
         {AccessCte}

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
         {AccessCte}

         DELETE FROM {TableName}
         WHERE {AccessFilter}
           AND {TableName}.id = @{nameof(GetSingleParameters.Id)};
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
            new GetSingleParameters(id, userId, Access.Read),
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleAsync<TEntity>(command);
    }

    /// <inheritdoc />
    public async ValueTask<TEntity> GetAsync(Guid id, Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            SelectAccessibleSingleSql,
            new GetSingleParameters(id, userId, Access.Read),
            transaction);

        return await Connection.QuerySingleAsync<TEntity>(command);
    }

    /// <inheritdoc />
    public async ValueTask<TEntity?> FindAsync(
        Guid id,
        Guid userId,
        Access access,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            SelectAccessibleSingleSql,
            new GetSingleParameters(id, userId, access),
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleOrDefaultAsync<TEntity>(command);
    }

    /// <inheritdoc />
    public async ValueTask<TEntity?> FindAsync(Guid id, Guid userId, Access access, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            SelectAccessibleSingleSql,
            new GetSingleParameters(id, userId, access),
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<TEntity>(command);
    }

    /// <inheritdoc />
    public async ValueTask<TEntity?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            SelectSingleSql,
            new GetSingleParameters(id, default, Access.Read),
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleOrDefaultAsync<TEntity>(command);
    }

    /// <inheritdoc />
    public async ValueTask<TEntity?> FindAsync(Guid id, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            SelectSingleSql,
            new GetSingleParameters(id, default, Access.Read),
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<TEntity>(command);
    }

    /// <inheritdoc />
    public async ValueTask<List<TEntity>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            SelectAccessibleSql,
            new GetParameters(userId, Access.Read),
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<TEntity>(command);
        return enumerable as List<TEntity> ?? enumerable.ToList();
    }

    /// <inheritdoc />
    public async ValueTask<List<TEntity>> GetAsync(Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            SelectAccessibleSql,
            new GetParameters(userId, Access.Read),
            transaction);

        var enumerable = await Connection.QueryAsync<TEntity>(command);
        return enumerable as List<TEntity> ?? enumerable.ToList();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TEntity> GetUnbufferedAsync(Guid userId)
    {
        return Connection.QueryUnbufferedAsync<TEntity>(
            SelectAccessibleSql,
            new GetParameters(userId, Access.Read));
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TEntity> GetUnbufferedAsync(Guid userId, NpgsqlTransaction transaction)
    {
        return Connection.QueryUnbufferedAsync<TEntity>(
            SelectAccessibleSql,
            new GetParameters(userId, Access.Read),
            transaction);
    }

    /// <inheritdoc />
    public async ValueTask<int> DeleteAsync(Guid id, Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(DeleteSql, new DeleteParameters(id, userId), transaction);
        return await Connection.ExecuteAsync(command);
    }
}
