DELETE FROM media.sponsorblock_segments
WHERE sponsorblock_segments.video_id IN (SELECT videos.id FROM media.videos WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
  AND sponsorblock_segments.id = @Id;
