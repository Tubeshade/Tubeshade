using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Tasks;

public sealed partial class ScheduleRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<ScheduleEntity>(connection)
{
    protected override string InsertSql => Queries.Insert;

    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;

    public async ValueTask<List<ScheduleEntity>> GetForSystemAsync(SystemPrincipal system, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(Queries.GetForSystem, cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<ScheduleEntity>(command);
        return enumerable as List<ScheduleEntity> ?? enumerable.ToList();
    }

    public async ValueTask<ScheduleEntity> GetForTask(
        long userId,
        Guid libraryId,
        TaskType type,
        Permissions permissions,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            Queries.GetForTask,
            Access.Parameters(new UserPrincipal(userId), Resources.Task, permissions | Permissions.Read, new { libraryId, type }),
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleAsync<ScheduleEntity>(command);
    }
}
