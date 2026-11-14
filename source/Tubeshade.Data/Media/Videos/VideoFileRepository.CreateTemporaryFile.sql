INSERT INTO media.video_files_downloading (file_id, task_run_id, path)
SELECT video_files.id, task_runs.id, @path
FROM media.video_files,
     tasks.task_runs
WHERE video_files.public_id = @id
  AND task_runs.public_id = @taskRunId
  AND video_files.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
ON CONFLICT (file_id) DO
    UPDATE SET task_run_id = excluded.task_run_id,
               path = excluded.path;
