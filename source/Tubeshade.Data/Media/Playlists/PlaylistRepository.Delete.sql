DELETE FROM media.playlists
WHERE playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
  AND playlists.id = @Id;
