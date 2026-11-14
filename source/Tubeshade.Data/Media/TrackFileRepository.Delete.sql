DELETE FROM media.track_files
WHERE track_files.video_id IN (SELECT videos.id
                               FROM media.videos
                                   INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
                               WHERE library_channels.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))))
  AND track_files.public_id = @Id;
