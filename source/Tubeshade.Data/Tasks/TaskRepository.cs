using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using NodaTime;
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
         INSERT INTO tasks.tasks (created_by_user_id, modified_by_user_id, owner_id, type, user_id, library_id, channel_id, video_id, url, all_videos) 
         VALUES (@{nameof(TaskEntity.CreatedByUserId)}, @{nameof(TaskEntity.ModifiedByUserId)}, @{nameof(TaskEntity.OwnerId)}, @{nameof(TaskEntity.Type)}, @{nameof(TaskEntity.UserId)}, @{nameof(TaskEntity.LibraryId)}, @{nameof(TaskEntity.ChannelId)}, @{nameof(TaskEntity.VideoId)}, @{nameof(TaskEntity.Url)}, @{nameof(TaskEntity.AllVideos)})
         RETURNING id;
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        $"""
         SELECT id AS {nameof(TaskEntity.Id)},
                created_at AS {nameof(TaskEntity.CreatedAt)},
                created_by_user_id AS {nameof(TaskEntity.CreatedByUserId)},
                modified_at AS {nameof(TaskEntity.ModifiedAt)},
                modified_by_user_id AS {nameof(TaskEntity.ModifiedByUserId)},
                owner_id AS {nameof(TaskEntity.OwnerId)},
                type AS {nameof(TaskEntity.Type)},
                user_id AS {nameof(TaskEntity.UserId)},
                library_id AS {nameof(TaskEntity.LibraryId)},
                channel_id AS {nameof(TaskEntity.ChannelId)},
                video_id AS {nameof(TaskEntity.VideoId)},
                url AS {nameof(TaskEntity.Url)},
                all_videos AS {nameof(TaskEntity.AllVideos)}
         FROM tasks.tasks
         """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        $"""
             type = @{nameof(TaskEntity.Type)},
             user_id = @{nameof(TaskEntity.UserId)},
             library_id = @{nameof(TaskEntity.LibraryId)},
             channel_id = @{nameof(TaskEntity.ChannelId)},
             video_id = @{nameof(TaskEntity.VideoId)},
             url = @{nameof(TaskEntity.Url)},
             all_videos = @{nameof(TaskEntity.AllVideos)}
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
            $"""
             INSERT INTO tasks.task_runs (task_id, state)
             VALUES (@{nameof(taskId)}, '{RunState.Names.Queued}')
             RETURNING id;
             """,
            new { taskId },
            transaction));
    }

    public async ValueTask StartTaskRun(Guid taskRunId, CancellationToken cancellationToken)
    {
        var count = await Connection.ExecuteAsync(new CommandDefinition(
            $"""
             UPDATE tasks.task_runs
             SET state = '{RunState.Names.Running}'
             WHERE id = @{nameof(taskRunId)};
             """,
            new { taskRunId },
            cancellationToken: cancellationToken));

        Trace.Assert(count is 1);
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
                  INNER JOIN media.libraries ON tasks.library_id = libraries.id
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
                  INNER JOIN media.libraries ON tasks.library_id = libraries.id
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

    public async ValueTask UpdateProgress(
        Guid taskRunId,
        decimal newValue,
        decimal? rate = null,
        Period? remainingDuration = null)
    {
        var count = await Connection.ExecuteAsync(new CommandDefinition(
            $"""
             UPDATE tasks.task_run_progress
             SET modified_at = CURRENT_TIMESTAMP,
                 value = @{nameof(newValue)},
                 rate = @{nameof(rate)},
                 remaining_duration = @{nameof(remainingDuration)}
             WHERE run_id = @{nameof(taskRunId)};
             """,
            new { taskRunId, newValue, rate, remainingDuration }));

        Trace.Assert(count is 1);
    }

    public async ValueTask<Guid> CompleteTask(Guid taskRunId, CancellationToken cancellationToken)
    {
        await using var transaction = await Connection.OpenAndBeginTransaction(cancellationToken);
        await FinishTaskRun(taskRunId, transaction);

        var id = await Connection.QuerySingleAsync<Guid>(new CommandDefinition(
            // lang=sql
            $"""
             INSERT INTO tasks.task_run_results (run_id, result, message)
             VALUES (@{nameof(taskRunId)}, @result, NULL)
             RETURNING id;
             """,
            new { taskRunId, result = TaskResult.Successful },
            transaction));

        await transaction.CommitAsync(cancellationToken);
        return id;
    }

    public async ValueTask<Guid> CancelledTask(Guid taskRunId, CancellationToken cancellationToken)
    {
        await using var transaction = await Connection.OpenAndBeginTransaction(cancellationToken);
        await FinishTaskRun(taskRunId, transaction);

        var id = await Connection.QuerySingleAsync<Guid>(new CommandDefinition(
            // lang=sql
            $"""
             INSERT INTO tasks.task_run_results (run_id, result, message)
             VALUES (@{nameof(taskRunId)}, @result, NULL)
             RETURNING id;
             """,
            new { taskRunId, result = TaskResult.Cancelled },
            transaction));

        await transaction.CommitAsync(cancellationToken);
        return id;
    }

    public async ValueTask<Guid> FailedTask(Guid taskRunId, Exception exception, CancellationToken cancellationToken)
    {
        await using var transaction = await Connection.OpenAndBeginTransaction(cancellationToken);
        await FinishTaskRun(taskRunId, transaction);

        var id = await Connection.QuerySingleAsync<Guid>(new CommandDefinition(
            // lang=sql
            $"""
             INSERT INTO tasks.task_run_results (run_id, result, message)
             VALUES (@{nameof(taskRunId)}, @result, @message)
             RETURNING id;
             """,
            new { taskRunId, result = TaskResult.Failed, message = exception.Message },
            transaction));

        await transaction.CommitAsync(cancellationToken);
        return id;
    }

    public async ValueTask<List<RunningTaskEntity>> GetRunningTasks(
        TaskParameters parameters,
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
                           ownerships.user_id = @{nameof(parameters.UserId)} AND
                           (ownerships.access = @{nameof(parameters.Access)} OR ownerships.access = 'owner'))

             SELECT filtered_tasks.id                    AS Id,
                    filtered_tasks.type                  AS Type,
                    filtered_tasks.library_id            AS LibraryId,
                    filtered_tasks.channel_id            AS ChannelId,
                    filtered_tasks.video_id              AS VideoId,
                    task_runs.id                         AS RunId,
                    task_runs.state                      AS RunState,
                    task_run_progress.value              AS Value,
                    task_run_progress.target             AS Target,
                    task_run_progress.rate               AS Rate,
                    task_run_progress.remaining_duration AS RemainingDuration,
                    task_run_results.result              AS Result,
                    task_run_results.message             AS Message,
                    CASE
                        WHEN filtered_tasks.type = 'index' AND filtered_tasks.video_id IS NOT NULL THEN
                            (SELECT name FROM media.videos WHERE id = filtered_tasks.video_id)

                        WHEN filtered_tasks.type = 'index' AND filtered_tasks.channel_id IS NOT NULL THEN
                            (SELECT name FROM media.channels WHERE id = filtered_tasks.channel_id)
                        
                        WHEN filtered_tasks.type = 'index' THEN
                            filtered_tasks.url

                        WHEN filtered_tasks.type = 'download_video' THEN
                            (SELECT name FROM media.videos WHERE id = filtered_tasks.video_id)

                        WHEN filtered_tasks.type = 'scan_channel' THEN
                            (SELECT name FROM media.channels WHERE id = filtered_tasks.channel_id)

                        ELSE
                            libraries.name
                        END                              AS Name,
                    count                                AS {nameof(RunningTaskEntity.TotalCount)}
             FROM (SELECT tasks.id,
                          tasks.type,
                          tasks.library_id,
                          tasks.channel_id,
                          tasks.video_id,
                          tasks.url,
                          tasks.created_at,
                          MAX(COALESCE(task_run_results.created_at, 'infinity')) AS result_created,
                          MAX(task_runs.created_at)        AS run_created,
                          count(*) OVER ()                 AS count
                   FROM tasks.tasks
                            INNER JOIN media.libraries ON tasks.library_id = libraries.id
                            INNER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
                            LEFT OUTER JOIN tasks.task_run_progress ON task_runs.id = task_run_progress.run_id
                            LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
                   WHERE (libraries.id IN (SELECT id FROM accessible))
                     AND (@{nameof(parameters.LibraryId)} IS NULL OR libraries.id = @{nameof(parameters.LibraryId)})
                     AND tasks.type != '{TaskType.Names.ReindexVideos}'
                   GROUP BY tasks.id, tasks.created_at
                   ORDER BY result_created DESC, run_created DESC, tasks.created_at DESC
                   OFFSET @{nameof(parameters.Offset)} LIMIT @{nameof(parameters.Limit)}) filtered_tasks
                      INNER JOIN media.libraries ON filtered_tasks.library_id = libraries.id
                      INNER JOIN tasks.task_runs ON filtered_tasks.id = task_runs.task_id
                      LEFT OUTER JOIN tasks.task_run_progress ON task_runs.id = task_run_progress.run_id
                      LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
             ORDER BY task_run_results.created_at DESC, task_runs.created_at DESC, filtered_tasks.created_at DESC;
             """,
            parameters,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<RunningTaskEntity>(command);
        return enumerable as List<RunningTaskEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<Guid>> GetBlockingTaskRunIds(TaskEntity task, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             WITH matching_videos AS
                      (SELECT id, external_url, channel_id
                       FROM media.videos
                       WHERE id = @{nameof(task.VideoId)}
                          OR external_url = @{nameof(task.Url)}),

                  matching_channels AS
                      (SELECT channels.id
                       FROM media.channels
                                LEFT OUTER JOIN matching_videos ON channel_id = channels.id
                       WHERE matching_videos.id IS NOT NULL
                          OR channels.id = @{nameof(task.ChannelId)})

             SELECT task_runs.id
             FROM tasks.tasks
                      INNER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
                      LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
             WHERE task_run_results.id IS NULL
               AND tasks.created_at < @{nameof(task.CreatedAt)}
               AND (tasks.video_id IN (SELECT id FROM matching_videos)
                 OR tasks.url IN (SELECT external_url FROM matching_videos)
                 OR tasks.channel_id IN (SELECT id FROM matching_channels)
                 OR (@{nameof(task.VideoId)} IS NOT NULL AND tasks.video_id = @{nameof(task.VideoId)})
                 OR (@{nameof(task.ChannelId)} IS NOT NULL AND tasks.channel_id = @{nameof(task.ChannelId)})
                 OR (@{nameof(task.Url)} IS NOT NULL AND tasks.url = @{nameof(task.Url)})
                 OR (@{nameof(task.Type)} = tasks.type AND tasks.type = '{TaskType.Names.ReindexVideos}'));
             """,
            task,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<Guid>(command);
        return enumerable as List<Guid> ?? enumerable.ToList();
    }

    public ValueTask<Guid> AddIndexTask(string url, Guid libraryId, Guid userId, NpgsqlTransaction transaction)
    {
        return AddTask(new TaskEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Type = TaskType.Index,
                UserId = userId,
                LibraryId = libraryId,
                ChannelId = null,
                VideoId = null,
                Url = url,
                AllVideos = false,
            },
            transaction);
    }

    public ValueTask<Guid> AddDownloadTask(Guid videoId, Guid libraryId, Guid userId, NpgsqlTransaction transaction)
    {
        return AddTask(new TaskEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Type = TaskType.DownloadVideo,
                UserId = userId,
                LibraryId = libraryId,
                ChannelId = null,
                VideoId = videoId,
                Url = null,
                AllVideos = false,
            },
            transaction);
    }

    public ValueTask<Guid> AddScanChannelTask(
        Guid libraryId,
        Guid channelId,
        bool allVideos,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        return AddTask(new TaskEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Type = TaskType.ScanChannel,
                UserId = userId,
                LibraryId = libraryId,
                ChannelId = channelId,
                VideoId = null,
                Url = null,
                AllVideos = allVideos,
            },
            transaction);
    }

    public ValueTask<Guid> AddScanSubscriptionsTask(Guid libraryId, Guid userId, NpgsqlTransaction transaction)
    {
        return AddTask(new TaskEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Type = TaskType.ScanSubscriptions,
                UserId = userId,
                LibraryId = libraryId,
                ChannelId = null,
                VideoId = null,
                Url = null,
                AllVideos = false,
            },
            transaction);
    }

    public ValueTask<Guid> AddScanSegmentsTask(Guid libraryId, Guid userId, NpgsqlTransaction transaction)
    {
        return AddTask(new TaskEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Type = TaskType.ScanSponsorBlockSegments,
                UserId = userId,
                LibraryId = libraryId,
                ChannelId = null,
                VideoId = null,
                Url = null,
                AllVideos = false,
            },
            transaction);
    }

    public async ValueTask<int> UpdateAsync(TaskEntity task)
    {
        var command = new CommandDefinition(UpdateSql, task);
        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask CompleteStuckTasks(CancellationToken cancellationToken)
    {
        await using var transaction = await Connection.OpenAndBeginTransaction(cancellationToken);

        var taskRunIds = await Connection.QueryAsync<Guid>(new(
            $"""
            SELECT id
            FROM tasks.task_runs
            WHERE state != '{RunState.Names.Finished}';
            """,
            new { },
            transaction));

        foreach (var taskRunId in taskRunIds)
        {
            await FinishTaskRun(taskRunId, transaction);
            await Connection.ExecuteAsync(new(
                $"""
                INSERT INTO tasks.task_run_results (run_id, result, message)
                VALUES (@{nameof(taskRunId)}, @result, 'Failed because server was stopped unexpectedly');
                """,
                new { taskRunId, result = TaskResult.Failed },
                transaction));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async ValueTask<Guid> AddTask(TaskEntity task, NpgsqlTransaction transaction)
    {
        var taskId = await AddAsync(task, transaction);
        Trace.Assert(taskId is not null);
        return taskId.Value;
    }

    private async ValueTask FinishTaskRun(Guid taskRunId, NpgsqlTransaction transaction)
    {
        await Connection.ExecuteAsync(new CommandDefinition(
            // lang=sql
            $"""
             UPDATE tasks.task_runs
             SET state = '{RunState.Names.Finished}'
             WHERE id = @{nameof(taskRunId)};

             SELECT pg_notify('{TaskChannels.RunFinished}', @{nameof(taskRunId)}::text);
             """,
            new { taskRunId },
            transaction));
    }
}
