INSERT INTO media.track_files (created_by_user_id, modified_by_user_id, video_id, storage_path, type, language, hash, hash_algorithm, storage_size)
SELECT @CreatedByUserId, @ModifiedByUserId, videos.id, @StoragePath, @Type, @Language, @Hash, @HashAlgorithm, @StorageSize
FROM media.videos
WHERE videos.public_id = @VideoId
  AND videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING public_id;
