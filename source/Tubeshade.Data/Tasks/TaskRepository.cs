using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.Tasks.Payloads;

namespace Tubeshade.Data.Tasks;

public sealed class TaskRepository(NpgsqlConnection connection) : ModifiableRepositoryBase<TaskEntity>(connection)
{
    private static readonly TaskPayloadContext Context = TaskPayloadContext.Default;

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

    public async ValueTask<TaskEntity?> TryDequeueTask(Guid taskId, NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleOrDefaultAsync<TaskEntity>(new CommandDefinition(
            $"""
             {SelectSql}
             WHERE tasks.id = @{nameof(taskId)}
               AND NOT EXISTS(SELECT
                              FROM tasks.task_runs
                                       LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
                              WHERE task_runs.task_id = tasks.id
                                AND task_run_results.id IS NULL)
             ORDER BY tasks.created_at
             LIMIT 1 FOR UPDATE SKIP LOCKED;
             """,
            new { taskId },
            transaction));
    }

    public async ValueTask<Guid> AddTaskRun(Guid taskId, NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleAsync<Guid>(new CommandDefinition(
            $"INSERT INTO tasks.task_runs (task_id) VALUES (@{nameof(taskId)}) RETURNING id;",
            new { taskId },
            transaction));
    }

    public async ValueTask TriggerTask(Guid taskId, NpgsqlTransaction transaction)
    {
        await Connection.ExecuteAsync(
            // lang=sql
            $"SELECT pg_notify('{TaskChannels.Created}', @{nameof(taskId)}::text);",
            new { taskId },
            transaction);
    }

    public async ValueTask TriggerTask(Guid taskId, Guid userId, NpgsqlTransaction transaction)
    {
        await Connection.ExecuteAsync(
            // lang=sql
            $"""
             WITH accessible AS
                 (SELECT libraries.id
                  FROM media.libraries
                  INNER JOIN identity.owners ON owners.id = libraries.owner_id
                  INNER JOIN identity.ownerships ON
                     ownerships.owner_id = owners.id AND
                     ownerships.user_id = @{nameof(userId)} AND
                     (ownerships.access = 'modify' OR ownerships.access = 'owner'))

             SELECT pg_notify('{TaskChannels.Created}', @{nameof(taskId)}::text)
             FROM tasks.tasks
                  INNER JOIN media.libraries ON (tasks.payload::json ->> 'libraryId')::uuid = libraries.id
             WHERE(libraries.id IN (SELECT id FROM accessible)) AND tasks.id = @{nameof(taskId)};
             """,
            new { taskId, userId },
            transaction);
    }

    public async ValueTask CancelTaskRun(Guid taskRunId, Guid userId, NpgsqlTransaction transaction)
    {
        await Connection.ExecuteAsync(
            // lang=sql
            $"""
             WITH accessible AS
                 (SELECT libraries.id
                  FROM media.libraries
                  INNER JOIN identity.owners ON owners.id = libraries.owner_id
                  INNER JOIN identity.ownerships ON
                     ownerships.owner_id = owners.id AND
                     ownerships.user_id = @{nameof(userId)} AND
                     (ownerships.access = 'modify' OR ownerships.access = 'owner'))

             SELECT pg_notify('{TaskChannels.Cancel}', @{nameof(taskRunId)}::text)
             FROM tasks.task_runs
                  INNER JOIN tasks.tasks ON task_runs.task_id = tasks.id
                  INNER JOIN media.libraries ON (tasks.payload::json ->> 'libraryId')::uuid = libraries.id
             WHERE (libraries.id IN (SELECT id FROM accessible)) AND task_runs.id = @{nameof(taskRunId)};
             """,
            new { taskRunId, userId },
            transaction);
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

    public async ValueTask<List<RunningTaskEntity>> GetRunningTasks(
        Guid libraryId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             WITH accessible AS
                 (SELECT libraries.id
                  FROM media.libraries
                  INNER JOIN identity.owners ON owners.id = libraries.owner_id
                  INNER JOIN identity.ownerships ON
                     ownerships.owner_id = owners.id AND
                     ownerships.user_id = @{nameof(userId)} AND
                     (ownerships.access = 'read' OR ownerships.access = 'owner'))
                     
             SELECT tasks.id                 AS Id,
                    tasks.type               AS Type,
                    tasks.payload            AS Payload,
                    task_runs.id             AS RunId,
                    task_run_progress.value  AS Value,
                    task_run_progress.target AS Target,
                    task_run_results.result  AS Result,
                    task_run_results.message AS Message
             FROM tasks.tasks
                      INNER JOIN media.libraries ON (tasks.payload::json ->> 'libraryId')::uuid = libraries.id
                      INNER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
                      LEFT OUTER JOIN tasks.task_run_progress ON task_runs.id = task_run_progress.run_id
                      LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
             WHERE
                 (libraries.id IN (SELECT id FROM accessible)) AND
                 libraries.id = @{nameof(libraryId)}
             ORDER BY task_run_results.created_at DESC, task_runs.created_at DESC, tasks.created_at DESC;
             """,
            new { libraryId, userId },
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<RunningTaskEntity>(command);
        return enumerable as List<RunningTaskEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<RunningTaskEntity>> GetRunningTasks(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             WITH accessible AS
                 (SELECT libraries.id
                  FROM media.libraries
                  INNER JOIN identity.owners ON owners.id = libraries.owner_id
                  INNER JOIN identity.ownerships ON
                     ownerships.owner_id = owners.id AND
                     ownerships.user_id = @{nameof(userId)} AND
                     (ownerships.access = 'read' OR ownerships.access = 'owner'))

             SELECT tasks.id                 AS Id,
                    tasks.type               AS Type,
                    tasks.payload            AS Payload,
                    task_runs.id             AS RunId,
                    task_run_progress.value  AS Value,
                    task_run_progress.target AS Target,
                    task_run_results.result  AS Result,
                    task_run_results.message AS Message,
                    CASE 
                        WHEN tasks.type = 'index' THEN
                        (tasks.payload::json ->> 'url')::text
                        
                        WHEN tasks.type = 'download_video' THEN
                        (SELECT name FROM media.videos WHERE id = (tasks.payload::json ->> 'videoId')::uuid)
                        
                        WHEN tasks.type = 'scan_channel' THEN
                        (SELECT name FROM media.channels WHERE id = (tasks.payload::json ->> 'channelId')::uuid)
                        
                        ELSE
                        libraries.name
                    END AS Name
             FROM tasks.tasks
                      INNER JOIN media.libraries ON (tasks.payload::json ->> 'libraryId')::uuid = libraries.id
                      INNER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
                      LEFT OUTER JOIN tasks.task_run_progress ON task_runs.id = task_run_progress.run_id
                      LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
             WHERE (libraries.id IN (SELECT id FROM accessible))
             ORDER BY task_run_results.created_at DESC, task_runs.created_at DESC, tasks.created_at DESC;
             """,
            new { userId },
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<RunningTaskEntity>(command);
        return enumerable as List<RunningTaskEntity> ?? enumerable.ToList();
    }

    public ValueTask<Guid> AddIndexTask(IndexPayload payload, Guid userId, NpgsqlTransaction transaction)
    {
        return AddTask(payload, Context.IndexPayload, userId, transaction);
    }

    public ValueTask<Guid> AddDownloadTask(DownloadVideoPayload payload, Guid userId, NpgsqlTransaction transaction)
    {
        return AddTask(payload, Context.DownloadVideoPayload, userId, transaction);
    }

    public ValueTask<Guid> AddScanChannelTask(ScanChannelPayload payload, Guid userId, NpgsqlTransaction transaction)
    {
        return AddTask(payload, Context.ScanChannelPayload, userId, transaction);
    }

    public ValueTask<Guid> AddScanSubscriptionsTask(ScanSubscriptionsPayload payload, Guid userId,
        NpgsqlTransaction transaction)
    {
        return AddTask(payload, Context.ScanSubscriptionsPayload, userId, transaction);
    }

    public ValueTask<Guid> AddScanSegmentsTask(ScanSponsorBlockSegmentsPayload payload, Guid userId,
        NpgsqlTransaction transaction)
    {
        return AddTask(payload, Context.ScanSponsorBlockSegmentsPayload, userId, transaction);
    }

    private async ValueTask<Guid> AddTask<TTaskPayload>(
        TTaskPayload payload,
        JsonTypeInfo<TTaskPayload> typeInfo,
        Guid userId,
        NpgsqlTransaction transaction)
        where TTaskPayload : PayloadBase, ITaskPayload
    {
        var type = TTaskPayload.TaskType;
        var payloadJson = JsonSerializer.Serialize(payload, typeInfo);

        var taskId = await AddAsync(new TaskEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Type = type,
                Payload = payloadJson,
            },
            transaction);

        Trace.Assert(taskId is not null);

        return taskId.Value;
    }
}
