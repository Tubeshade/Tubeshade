INSERT INTO media.sponsorblock_segments (created_by_user_id, modified_by_user_id, video_id, external_id, start_time, end_time, category, action, locked, description)
SELECT @CreatedByUserId, @ModifiedByUserId, @VideoId, @ExternalId, @StartTime, @EndTime, @Category, @Action, @Locked, @Description
WHERE @VideoId IN (SELECT videos.id
                   FROM media.videos
                   WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions)))
RETURNING id;
