INSERT INTO media.playlists (created_by_user_id, modified_by_user_id, library_id, storage_path, external_id, external_url, name, refreshed_at, subscribed_at)
SELECT @CreatedByUserId, @ModifiedByUserId, @LibraryId, @StoragePath, @ExternalId, @ExternalUrl, @Name, @RefreshedAt, @SubscribedAt
WHERE @LibraryId IN (SELECT libraries.id
                     FROM media.libraries
                     WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions)))
RETURNING id;
