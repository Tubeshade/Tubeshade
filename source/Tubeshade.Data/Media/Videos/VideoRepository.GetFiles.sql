SELECT video_files.id,
       video_files.public_id,
       video_files.created_at,
       video_files.created_by_user_id,
       video_files.modified_at,
       video_files.modified_by_user_id,
       video_files.owner_id,
       videos.public_id AS video_id,
       video_files.storage_path,
       video_files.type,
       video_files.width,
       video_files.height,
       video_files.framerate,
       video_files.downloaded_at,
       video_files.downloaded_by_user_id,
       video_files.hash_algorithm,
       video_files.hash,
       video_files.storage_size,
       (SELECT task_runs.public_id FROM tasks.task_runs WHERE task_runs.id = downloading.task_run_id) AS task_run_id,
       downloading.path AS TempPath
FROM media.video_files
   INNER JOIN media.videos ON video_files.video_id = videos.id
   LEFT OUTER JOIN media.video_files_downloading downloading ON video_files.id = downloading.file_id
WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)) AND videos.public_id = @VideoId
ORDER BY video_files.width DESC, video_files.framerate DESC, video_files.id;
