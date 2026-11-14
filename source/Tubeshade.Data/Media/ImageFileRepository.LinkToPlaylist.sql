INSERT INTO media.playlist_images (playlist_id, image_id)
SELECT playlists.id, @id
FROM media.playlists
WHERE playlists.id = @playlistId
  AND playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)));
