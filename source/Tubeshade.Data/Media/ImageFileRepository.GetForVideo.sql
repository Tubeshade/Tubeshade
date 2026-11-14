SELECT id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       storage_path,
       type,
       width,
       height,
       hash_algorithm,
       hash,
       storage_size
FROM media.image_files
    INNER JOIN media.video_images ON image_files.id = video_images.image_id
WHERE video_images.video_id IN (SELECT videos.id
                                FROM media.videos
                                    INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
                                WHERE videos.id = @VideoId
                                  AND library_channels.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))))
ORDER BY image_files.width, image_files.height;
