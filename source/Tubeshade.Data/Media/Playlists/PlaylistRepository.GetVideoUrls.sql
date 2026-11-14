SELECT videos.external_url
FROM media.playlists
INNER JOIN media.playlist_videos ON playlists.id = playlist_videos.playlist_id
INNER JOIN media.videos ON playlist_videos.video_id = videos.id
WHERE playlists.public_id = @playlistId
  AND playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
ORDER BY playlist_videos."order";
