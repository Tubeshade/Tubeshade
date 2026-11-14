SELECT track_files.id,
       track_files.created_at,
       track_files.created_by_user_id,
       track_files.modified_at,
       track_files.modified_by_user_id,
       track_files.video_id,
       track_files.storage_path,
       track_files.type,
       track_files.language,
       track_files.hash,
       track_files.hash_algorithm,
       track_files.storage_size
FROM media.track_files
