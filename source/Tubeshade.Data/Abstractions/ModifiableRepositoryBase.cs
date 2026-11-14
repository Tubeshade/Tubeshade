using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.AccessControl;

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
         UPDATE {TableName}
         SET modified_at = CURRENT_TIMESTAMP,
             modified_by_user_id = @{nameof(IModifiableEntity.ModifiedByUserId)},
         {UpdateSet}

         WHERE {UpdateAccessFilter}
           AND ({TableName}.id = @{nameof(IEntity.Id)});
         """;

    protected virtual string UpdateAccessFilter =>
        $"""
         ({TableName}.owner_id IN ({UpdatableOwners}))
         """;

    protected static string UpdatableOwners =>
        $"SELECT owner_id FROM access.accessible_owners(@{nameof(IModifiableEntity.ModifiedByUserId)}, {(int)Permissions.Update})";

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
