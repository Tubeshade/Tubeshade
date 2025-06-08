using System.Threading.Tasks;
using Npgsql;

namespace Tubeshade.Data.Abstractions;

public interface IModifiableRepository<TEntity> : IRepository<TEntity>
    where TEntity : IModifiableEntity
{
    ValueTask<int> UpdateAsync(TEntity entity, NpgsqlTransaction transaction);

    ValueTask<int> DeleteAsync(TEntity entity, NpgsqlTransaction transaction);
}
