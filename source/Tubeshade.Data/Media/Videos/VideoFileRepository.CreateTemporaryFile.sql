INSERT INTO media.video_files_downloading (file_id, task_run_id, path)
SELECT video_files.id, @taskRunId, @path
FROM media.video_files
WHERE video_files.id = @id
  AND video_files.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
ON CONFLICT (file_id) DO
    UPDATE SET task_run_id = @taskRunId,
               path = @path;
