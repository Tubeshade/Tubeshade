-- An image is only reachable once it is linked to a video, channel or playlist, so there is no access check here
INSERT INTO media.image_files (created_by_user_id, modified_by_user_id, storage_path, type, width, height, hash_algorithm, hash, storage_size)
VALUES (@CreatedByUserId, @ModifiedByUserId, @StoragePath, @Type, @Width, @Height, @HashAlgorithm, @Hash, @StorageSize)
RETURNING public_id;
