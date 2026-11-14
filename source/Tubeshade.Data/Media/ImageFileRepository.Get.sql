SELECT id,
       public_id,
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
WHERE image_files.id IN (SELECT video_images.image_id
                         FROM media.videos
                             INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
                             INNER JOIN media.video_images ON videos.id = video_images.video_id
                         WHERE library_channels.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))

                         UNION ALL

                         SELECT channel_images.image_id
                         FROM media.library_channels
                             INNER JOIN media.channel_images ON library_channels.channel_id = channel_images.channel_id
                         WHERE library_channels."primary"
                           AND library_channels.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))

                         UNION ALL

                         SELECT playlist_images.image_id
                         FROM media.playlists
                             INNER JOIN media.playlist_images ON playlists.id = playlist_images.playlist_id
                         WHERE playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))))
