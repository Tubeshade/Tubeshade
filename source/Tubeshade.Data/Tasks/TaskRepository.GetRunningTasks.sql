SELECT filtered_tasks.public_id                AS id,
       filtered_tasks.type,
       libraries.public_id                     AS library_id,
       (SELECT public_id FROM media.channels WHERE id = filtered_tasks.channel_id) AS channel_id,
       (SELECT public_id FROM media.playlists WHERE id = filtered_tasks.playlist_id) AS playlist_id,
       (SELECT public_id FROM media.videos WHERE id = filtered_tasks.video_id) AS video_id,
       filtered_tasks.url,
       task_runs.public_id                  AS RunId,
       task_runs.state                      AS RunState,
       task_runs.source,
       task_runs.created_at                 AS RunCreatedAt,
       task_run_progress.value,
       task_run_progress.target,
       task_run_progress.rate,
       task_run_progress.remaining_duration,
       task_run_results.result,
       task_run_results.message,
       CASE
           WHEN filtered_tasks.type = 'index' AND filtered_tasks.video_id IS NOT NULL THEN
               (SELECT name FROM media.videos WHERE id = filtered_tasks.video_id)

           WHEN filtered_tasks.type = 'index' AND filtered_tasks.playlist_id IS NOT NULL THEN
               (SELECT name FROM media.playlists WHERE id = filtered_tasks.playlist_id)

           WHEN filtered_tasks.type = 'index' AND filtered_tasks.channel_id IS NOT NULL AND task_runs.source != 'webhook' THEN
               (SELECT name FROM media.channels WHERE id = filtered_tasks.channel_id)

           WHEN filtered_tasks.type = 'index' THEN
               filtered_tasks.url

           WHEN filtered_tasks.type = 'download_video' THEN
               (SELECT name FROM media.videos WHERE id = filtered_tasks.video_id)

           WHEN filtered_tasks.type = 'scan_channel' THEN
               (SELECT name FROM media.channels WHERE id = filtered_tasks.channel_id)

           WHEN filtered_tasks.type = 'scan_playlist' THEN
               (SELECT name FROM media.playlists WHERE id = filtered_tasks.playlist_id)

           WHEN filtered_tasks.type = 'youtube_feed_update' THEN
               (SELECT name FROM media.channels WHERE id = filtered_tasks.channel_id)

           ELSE
               libraries.name
           END                              AS Name,
       count                                AS TotalCount
FROM (SELECT tasks.id,
             tasks.public_id,
             tasks.type,
             tasks.library_id,
             tasks.channel_id,
             tasks.playlist_id,
             tasks.video_id,
             tasks.url,
             tasks.created_at,
             MAX(COALESCE(task_run_results.created_at, 'infinity')) AS result_created,
             MAX(task_runs.created_at)        AS run_created,
             count(*) OVER ()                 AS count
      FROM tasks.tasks
               INNER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
               LEFT OUTER JOIN tasks.task_run_progress ON task_runs.id = task_run_progress.run_id
               LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
      WHERE (tasks.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
        AND (@LibraryId IS NULL OR tasks.library_id = (SELECT libraries.id FROM media.libraries WHERE libraries.public_id = @LibraryId))
        -- finished feed updates are hidden unless filtered by source, but unfinished ones are shown since they take up workers
        AND ((@Source::tasks.task_source IS NULL AND (tasks.type != 'youtube_feed_update' OR task_run_results.id IS NULL OR @TaskRunId IS NOT NULL)) OR task_runs.source = @Source)
        AND (@State::tasks.run_state IS NULL OR task_runs.state = @State)
        AND (@Result::tasks.task_result IS NULL OR task_run_results.result = @Result)
        AND (@TaskRunId IS NULL OR task_runs.public_id = @TaskRunId)
        AND (@Type::tasks.task_type IS NULL OR tasks.type = @Type)
        AND (@Url IS NULL OR tasks.url = @Url)
        AND tasks.type != 'reindex_videos'
      GROUP BY tasks.id, tasks.created_at
      ORDER BY result_created DESC, run_created DESC, tasks.created_at DESC
      OFFSET @Offset LIMIT @Limit) filtered_tasks
         LEFT OUTER JOIN media.libraries ON filtered_tasks.library_id = libraries.id
         INNER JOIN tasks.task_runs ON filtered_tasks.id = task_runs.task_id
         LEFT OUTER JOIN tasks.task_run_progress ON task_runs.id = task_run_progress.run_id
         LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
ORDER BY task_run_results.created_at DESC, task_runs.created_at DESC, filtered_tasks.created_at DESC;
