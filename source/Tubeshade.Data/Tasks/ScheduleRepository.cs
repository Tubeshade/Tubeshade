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

public sealed class ScheduleRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<ScheduleEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "tasks.schedules";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO tasks.schedules (created_by_user_id, modified_by_user_id, owner_id, task_id, cron_expression, time_zone_id)
         SELECT @CreatedByUserId, @ModifiedByUserId, @OwnerId, @TaskId, @CronExpression, @TimeZoneId
         WHERE {CanCreateIn(nameof(ScheduleEntity.OwnerId))}
         RETURNING id;
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        """
        SELECT id,
               created_at,
               created_by_user_id,
               modified_at,
               modified_by_user_id,
               owner_id,
               task_id,
               cron_expression,
               time_zone_id
        FROM tasks.schedules
        """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        """
        task_id = @TaskId,
        cron_expression = @CronExpression,
        time_zone_id = @TimeZoneId
        """;

    public async ValueTask<List<ScheduleEntity>> GetForSystemAsync(SystemPrincipal system, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(SelectSql, cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<ScheduleEntity>(command);
        return enumerable as List<ScheduleEntity> ?? enumerable.ToList();
    }

    public async ValueTask<ScheduleEntity> GetForTask(
        Guid userId,
        Guid libraryId,
        TaskType type,
        Permissions permissions,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             SELECT schedules.id,
                    schedules.created_at,
                    schedules.created_by_user_id,
                    schedules.modified_at,
                    schedules.modified_by_user_id,
                    schedules.owner_id,
                    schedules.task_id,
                    schedules.cron_expression,
                    schedules.time_zone_id
             FROM tasks.schedules
                 INNER JOIN tasks.tasks ON schedules.task_id = tasks.id
             WHERE schedules.owner_id IN ({Access.AccessibleOwners}) AND
                   tasks.type = @{nameof(type)} AND
                   tasks.library_id = @{nameof(libraryId)}
             """,
            Access.Parameters(new UserPrincipal(userId), Resources.Task, permissions | Permissions.Read, new { libraryId, type }),
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleAsync<ScheduleEntity>(command);
    }
}
