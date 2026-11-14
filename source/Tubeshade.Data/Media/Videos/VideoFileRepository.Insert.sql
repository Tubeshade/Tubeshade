INSERT INTO media.video_files (created_by_user_id, modified_by_user_id, owner_id, video_id, storage_path, type, width, height, framerate, downloaded_at, downloaded_by_user_id, hash_algorithm, hash, storage_size)
SELECT @CreatedByUserId, @ModifiedByUserId, videos.owner_id, videos.id, @StoragePath, @Type, @Width, @Height, @Framerate, @DownloadedAt, @DownloadedByUserId, @HashAlgorithm, @Hash, @StorageSize
FROM media.videos
WHERE videos.public_id = @VideoId
  AND videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING public_id;
