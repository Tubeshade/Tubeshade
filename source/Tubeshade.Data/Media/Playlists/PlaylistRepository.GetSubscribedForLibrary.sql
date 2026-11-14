SELECT id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       library_id,
       storage_path,
       external_id,
       external_url,
       name,
       refreshed_at,
       subscribed_at
FROM media.playlists
WHERE playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
  AND playlists.library_id = @LibraryId
  AND playlists.subscribed_at IS NOT NULL;
