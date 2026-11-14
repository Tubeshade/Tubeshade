SELECT channels.storage_path
FROM media.channels
   INNER JOIN media.channel_images ON channels.id = channel_images.channel_id
WHERE channel_images.image_id = @Id
  AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))

UNION ALL

SELECT videos.storage_path
FROM media.videos
   INNER JOIN media.video_images ON videos.id = video_images.video_id
WHERE video_images.image_id = @Id
  AND videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))

UNION ALL

SELECT playlists.storage_path
FROM media.playlists
   INNER JOIN media.playlist_images ON playlists.id = playlist_images.playlist_id
WHERE playlist_images.image_id = @Id
  AND playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)));
