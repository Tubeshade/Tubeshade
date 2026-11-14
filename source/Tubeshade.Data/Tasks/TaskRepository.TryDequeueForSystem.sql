SELECT id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       owner_id,
       type,
       user_id,
       library_id,
       channel_id,
       playlist_id,
       video_id,
       url,
       all_videos,
       payload
FROM tasks.tasks
WHERE tasks.id = @taskId
  AND NOT EXISTS(SELECT
                 FROM tasks.task_runs
                          LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
                 WHERE task_runs.task_id = tasks.id
                   AND task_run_results.id IS NULL)
