using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Tasks;

public sealed class TaskRepository(NpgsqlConnection connection) : ModifiableRepositoryBase<TaskEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "tasks.tasks";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO tasks.tasks (created_by_user_id, modified_by_user_id, owner_id, type, payload) 
         VALUES (@CreatedByUserId, @ModifiedByUserId, @OwnerId, @Type, @Payload)
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
                type AS Type,
                payload AS Payload
         FROM tasks.tasks
         """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        $"""
             type = @{nameof(TaskEntity.Type)},
             payload = @{nameof(TaskEntity.Payload)}
         """;

    public async ValueTask<TaskEntity?> TryDequeueTask(Guid taskId, TaskType taskType, NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleOrDefaultAsync<TaskEntity>(new CommandDefinition(
            $"""
             {SelectSql}
             WHERE tasks.id = @{nameof(taskId)} AND tasks.type = @{nameof(taskType)}
               AND NOT EXISTS(SELECT
                              FROM tasks.task_runs
                                       LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
                              WHERE task_runs.task_id = tasks.id
                                AND task_run_results.id IS NULL)
             ORDER BY tasks.created_at
             LIMIT 1 FOR UPDATE SKIP LOCKED;
             """,
            new { taskId, taskType },
            transaction));
    }

    public async ValueTask<Guid> StartTask(Guid taskId, NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleAsync<Guid>(new CommandDefinition(
            $"INSERT INTO tasks.task_runs (task_id) VALUES (@{nameof(taskId)}) RETURNING id;",
            new { taskId },
            transaction));
    }

    public async ValueTask InitializeTaskProgress(Guid taskRunId, decimal target)
    {
        await Connection.ExecuteAsync(new CommandDefinition(
            $"""
             INSERT INTO tasks.task_run_progress (run_id, value, target) 
             VALUES (@{nameof(taskRunId)}, 0, @{nameof(target)});
             """,
            new { taskRunId, target }));
    }

    public async ValueTask UpdateProgress(Guid taskRunId, decimal newValue)
    {
        var count = await Connection.ExecuteAsync(new CommandDefinition(
            $"""
             UPDATE tasks.task_run_progress
             SET modified_at = CURRENT_TIMESTAMP,
                 value = @{nameof(newValue)}
             WHERE run_id = @{nameof(taskRunId)};
             """,
            new { taskRunId, newValue }));

        Trace.Assert(count is 1);
    }

    public async ValueTask<Guid> CompleteTask(Guid taskRunId, CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleAsync<Guid>(new CommandDefinition(
            $"""
             INSERT INTO tasks.task_run_results (run_id, result, message)
             VALUES (@{nameof(taskRunId)}, @result, NULL)
             RETURNING id;
             """,
            new { taskRunId, result = TaskResult.Successful },
            cancellationToken: cancellationToken));
    }

    public async ValueTask<Guid> CancelledTask(Guid taskRunId, CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleAsync<Guid>(new CommandDefinition(
            $"""
             INSERT INTO tasks.task_run_results (run_id, result, message)
             VALUES (@{nameof(taskRunId)}, @result, NULL)
             RETURNING id;
             """,
            new { taskRunId, result = TaskResult.Cancelled },
            cancellationToken: cancellationToken));
    }

    public async ValueTask<Guid> FailedTask(Guid taskRunId, Exception exception, CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleAsync<Guid>(new CommandDefinition(
            $"""
             INSERT INTO tasks.task_run_results (run_id, result, message)
             VALUES (@{nameof(taskRunId)}, @result, @message)
             RETURNING id;
             """,
            new { taskRunId, result = TaskResult.Failed, message = exception.Message },
            cancellationToken: cancellationToken));
    }
}
