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
    INNER JOIN media.playlist_images ON image_files.id = playlist_images.image_id
WHERE playlist_images.playlist_id IN (SELECT playlists.id
                                      FROM media.playlists
                                      WHERE playlists.public_id = @VideoId
                                        AND playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))))
ORDER BY image_files.width, image_files.height;
