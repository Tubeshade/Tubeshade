INSERT INTO media.playlist_images (playlist_id, image_id)
SELECT playlists.id, image_files.id
FROM media.playlists,
     media.image_files
WHERE playlists.public_id = @playlistId
  AND image_files.public_id = @id
  AND playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)));
