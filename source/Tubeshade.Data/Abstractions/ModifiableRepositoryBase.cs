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

    protected abstract string UpdateSql { get; }

    /// <inheritdoc />
    public async ValueTask<int> UpdateAsync(TEntity entity, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(UpdateSql, Access.WithPermissions(entity, Permissions.Update), transaction);
        return await Connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public ValueTask<int> DeleteAsync(TEntity entity, NpgsqlTransaction transaction)
    {
        return DeleteAsync(entity.Id, entity.ModifiedByUserId, transaction);
    }
}
