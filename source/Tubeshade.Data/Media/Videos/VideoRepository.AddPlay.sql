INSERT INTO media.video_plays (video_id, user_id)
SELECT videos.id, @PrincipalId
FROM media.videos
WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)) AND videos.public_id = @VideoId;
