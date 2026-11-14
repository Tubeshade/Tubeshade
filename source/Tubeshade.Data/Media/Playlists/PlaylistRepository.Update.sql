UPDATE media.playlists
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    library_id = (SELECT libraries.id FROM media.libraries WHERE libraries.public_id = @LibraryId),
    storage_path = @StoragePath,
    external_id = @ExternalId,
    external_url = @ExternalUrl,
    name = @Name,
    refreshed_at = @RefreshedAt,
    subscribed_at = @SubscribedAt
WHERE playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions)))
  AND playlists.public_id = @PublicId;
