UPDATE media.track_files
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    video_id = (SELECT videos.id FROM media.videos WHERE videos.public_id = @VideoId),
    storage_path = @StoragePath,
    type = @Type,
    language = @Language,
    hash_algorithm = @HashAlgorithm,
    hash = @Hash,
    storage_size = @StorageSize
WHERE track_files.video_id IN (SELECT videos.id
                               FROM media.videos
                                   INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
                               WHERE library_channels.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))))
  AND track_files.public_id = @PublicId;
