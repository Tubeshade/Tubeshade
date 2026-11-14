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
    INNER JOIN media.channel_images ON image_files.id = channel_images.image_id
WHERE channel_images.channel_id IN (SELECT library_channels.channel_id
                                    FROM media.library_channels
                                    WHERE library_channels.channel_id = @VideoId
                                      AND library_channels."primary"
                                      AND library_channels.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))))
ORDER BY image_files.width, image_files.height;
