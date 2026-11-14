INSERT INTO media.playlists (created_by_user_id, modified_by_user_id, library_id, storage_path, external_id, external_url, name, refreshed_at, subscribed_at)
SELECT @CreatedByUserId, @ModifiedByUserId, libraries.id, @StoragePath, @ExternalId, @ExternalUrl, @Name, @RefreshedAt, @SubscribedAt
FROM media.libraries
WHERE libraries.public_id = @LibraryId
  AND libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING public_id;
