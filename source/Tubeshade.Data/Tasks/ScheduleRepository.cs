using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;

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
}
