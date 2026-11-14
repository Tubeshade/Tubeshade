INSERT INTO media.video_viewed_by_users (video_id, user_id, viewed)
SELECT videos.id, @PrincipalId, @viewed
FROM media.videos
WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)) AND videos.public_id = @VideoId
ON CONFLICT (video_id, user_id) DO UPDATE
SET modified_at = CURRENT_TIMESTAMP,
    viewed = excluded.viewed;
