INSERT INTO media.video_images (video_id, image_id)
SELECT videos.id, image_files.id
FROM media.videos,
     media.image_files
WHERE videos.public_id = @videoId
  AND image_files.public_id = @id
  AND videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
