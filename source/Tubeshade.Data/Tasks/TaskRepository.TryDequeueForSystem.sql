SELECT id,
       public_id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       owner_id,
       type,
       user_id,
       (SELECT libraries.public_id FROM media.libraries WHERE libraries.id = tasks.library_id) AS library_id,
       (SELECT channels.public_id FROM media.channels WHERE channels.id = tasks.channel_id) AS channel_id,
       (SELECT playlists.public_id FROM media.playlists WHERE playlists.id = tasks.playlist_id) AS playlist_id,
       (SELECT videos.public_id FROM media.videos WHERE videos.id = tasks.video_id) AS video_id,
       url,
       all_videos,
       payload
FROM tasks.tasks
WHERE tasks.public_id = @taskId
  AND NOT EXISTS(SELECT
                 FROM tasks.task_runs
                          LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
                 WHERE task_runs.task_id = tasks.id
                   AND task_run_results.id IS NULL)
