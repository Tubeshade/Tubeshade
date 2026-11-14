INSERT INTO media.sponsorblock_segments (created_by_user_id, modified_by_user_id, video_id, external_id, start_time, end_time, category, action, locked, description)
SELECT @CreatedByUserId, @ModifiedByUserId, videos.id, @ExternalId, @StartTime, @EndTime, @Category, @Action, @Locked, @Description
FROM media.videos
WHERE videos.public_id = @VideoId
  AND videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING public_id;
