using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Tubeshade.Data.Abstractions;

public abstract class ModifiableRepositoryBase<TEntity> : RepositoryBase<TEntity>, IModifiableRepository<TEntity>
    where TEntity : IModifiableEntity
{
    protected ModifiableRepositoryBase(NpgsqlConnection connection)
        : base(connection)
    {
    }

    protected abstract string UpdateSet { get; }

    protected virtual string UpdateSql =>
        $"""
         {UpdateAccessCte}

         UPDATE {TableName}
         SET modified_at = CURRENT_TIMESTAMP,
             modified_by_user_id = @{nameof(IModifiableEntity.ModifiedByUserId)},
         {UpdateSet}

         WHERE {AccessFilter}
           AND ({TableName}.id = @{nameof(IEntity.Id)});
         """;

    protected virtual string UpdateAccessCte =>
        $"""
         WITH accessible AS
         (SELECT {TableName}.id
          FROM {TableName}
                   INNER JOIN identity.owners ON owners.id = {TableName}.owner_id
                   INNER JOIN identity.ownerships ON
              ownerships.owner_id = owners.id AND
              ownerships.user_id = @{nameof(IModifiableEntity.ModifiedByUserId)} AND
              (ownerships.access = @{nameof(IModifiableEntity.AccessFoo)} OR ownerships.access = 'owner'))
         """;


    /// <inheritdoc />
    public async ValueTask<int> UpdateAsync(TEntity entity, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(UpdateSql, entity, transaction);
        return await Connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public ValueTask<int> DeleteAsync(TEntity entity, NpgsqlTransaction transaction)
    {
        return DeleteAsync(entity.Id, entity.ModifiedByUserId, transaction);
    }
}
