INSERT INTO media.video_viewed_by_users (video_id, user_id, viewed, position, watch_time, playback_time)
SELECT videos.id, @PrincipalId, false, @position, @watchTime, @playbackTime
FROM media.videos
WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)) AND videos.id = @VideoId
ON CONFLICT (video_id, user_id) DO UPDATE
SET modified_at = CURRENT_TIMESTAMP,
    position = excluded.position,
    watch_time = video_viewed_by_users.watch_time + excluded.watch_time,
    playback_time = video_viewed_by_users.playback_time + excluded.playback_time;
