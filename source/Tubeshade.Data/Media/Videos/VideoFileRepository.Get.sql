SELECT id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       owner_id,
       video_id,
       storage_path,
       type,
       width,
       height,
       framerate,
       downloaded_at,
       downloaded_by_user_id,
       hash_algorithm,
       hash,
       storage_size
FROM media.video_files
WHERE video_files.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
