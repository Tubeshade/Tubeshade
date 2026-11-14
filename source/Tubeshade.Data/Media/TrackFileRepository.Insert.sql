INSERT INTO media.track_files (created_by_user_id, modified_by_user_id, video_id, storage_path, type, language, hash, hash_algorithm, storage_size)
SELECT @CreatedByUserId, @ModifiedByUserId, @VideoId, @StoragePath, @Type, @Language, @Hash, @HashAlgorithm, @StorageSize
WHERE @VideoId IN (SELECT videos.id
                   FROM media.videos
                   WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions)))
RETURNING id;
