UPDATE media.image_files
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    storage_path = @StoragePath,
    type = @Type,
    width = @Width,
    height = @Height,
    hash_algorithm = @HashAlgorithm,
    hash = @Hash,
    storage_size = @StorageSize
WHERE image_files.public_id = @PublicId
  AND (EXISTS (SELECT
               FROM media.video_images
                   INNER JOIN media.videos ON videos.id = video_images.video_id
                   INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
               WHERE video_images.image_id = image_files.id
                 AND library_channels.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))))
    OR EXISTS (SELECT
               FROM media.channel_images
                   INNER JOIN media.library_channels ON library_channels.channel_id = channel_images.channel_id AND library_channels."primary"
               WHERE channel_images.image_id = image_files.id
                 AND library_channels.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))))
    OR EXISTS (SELECT
               FROM media.playlist_images
                   INNER JOIN media.playlists ON playlists.id = playlist_images.playlist_id
               WHERE playlist_images.image_id = image_files.id
                 AND playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions)))));
