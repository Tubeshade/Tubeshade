INSERT INTO media.video_images (video_id, image_id)
SELECT videos.id, @id
FROM media.videos
WHERE videos.id = @videoId
  AND videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
