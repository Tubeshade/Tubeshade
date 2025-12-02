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
         VALUES (@CreatedByUserId, @ModifiedByUserId, @OwnerId, @TaskId, @CronExpression, @TimeZoneId)
         RETURNING id;
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        $"""
         SELECT id AS Id,
                created_at AS CreatedAt,
                created_by_user_id AS CreatedByUserId,
                modified_at AS ModifiedAt,
                modified_by_user_id AS ModifiedByUserId,
                owner_id AS OwnerId,
                task_id AS TaskId,
                cron_expression AS CronExpression,
                time_zone_id AS TimeZoneId
         FROM tasks.schedules
         """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        """
        task_id = @TaskId,
        cron_expression = @CronExpression,
        time_zone_id = @TimeZoneId
        """;

    public async ValueTask<List<ScheduleEntity>> GetWithoutAccessControl(CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(SelectSql, cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<ScheduleEntity>(command);
        return enumerable as List<ScheduleEntity> ?? enumerable.ToList();
    }

    public async ValueTask<ScheduleEntity> GetForTask(
        Guid userId,
        Guid libraryId,
        TaskType type,
        Access access,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             WITH accessible AS 
                 (SELECT libraries.id
                  FROM media.libraries
                      INNER JOIN identity.owners ON owners.id = media.libraries.owner_id
                      INNER JOIN identity.ownerships ON
                          ownerships.owner_id = owners.id AND
                          ownerships.user_id = @{nameof(userId)} AND
                          (ownerships.access = @{nameof(access)} OR ownerships.access = 'owner'))

             SELECT schedules.id                  AS Id,
                    schedules.created_at          AS CreatedAt,
                    schedules.created_by_user_id  AS CreatedByUserId,
                    schedules.modified_at         AS ModifiedAt,
                    schedules.modified_by_user_id AS ModifiedByUserId,
                    schedules.owner_id            AS OwnerId,
                    schedules.task_id             AS TaskId,
                    schedules.cron_expression     AS CronExpression,
                    schedules.time_zone_id        AS TimeZoneId
             FROM tasks.schedules
                 INNER JOIN tasks.tasks ON schedules.task_id = tasks.id
                 INNER JOIN accessible ON tasks.library_id = accessible.id
             WHERE tasks.type = @{nameof(type)} AND
                   tasks.library_id = @{nameof(libraryId)}
             """,
            new { userId, libraryId, type, access },
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleAsync<ScheduleEntity>(command);
    }
}
