SELECT videos.id,
       videos.external_id
FROM media.videos
         INNER JOIN media.channels ON videos.channel_id = channels.id
         INNER JOIN media.library_channels ON channels.id = library_channels.channel_id
         LEFT OUTER JOIN media.sponsorblock_segments ON videos.id = sponsorblock_segments.video_id
WHERE library_id = @LibraryId
  AND videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND sponsorblock_segments.video_id IS NULL;
