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
WHERE track_files.video_id IN (SELECT videos.id
                               FROM media.videos
                                   INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
                               WHERE videos.id = @VideoId
                                 AND library_channels.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))));
