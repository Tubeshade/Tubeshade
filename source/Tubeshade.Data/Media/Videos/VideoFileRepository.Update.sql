UPDATE media.video_files
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    video_id = @VideoId,
    storage_path = @StoragePath,
    type = @Type,
    width = @Width,
    height = @Height,
    framerate = @Framerate,
    downloaded_at = @DownloadedAt,
    downloaded_by_user_id = @DownloadedByUserId,
    hash_algorithm = @HashAlgorithm,
    hash = @Hash,
    storage_size = @StorageSize
WHERE video_files.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))
  AND video_files.id = @Id;
