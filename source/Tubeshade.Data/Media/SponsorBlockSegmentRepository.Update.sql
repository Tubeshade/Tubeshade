UPDATE media.sponsorblock_segments
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    start_time = @StartTime,
    end_time = @EndTime,
    category = @Category,
    action = @Action,
    locked = @Locked,
    description = @Description
WHERE sponsorblock_segments.video_id IN (SELECT videos.id FROM media.videos WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions)))
  AND sponsorblock_segments.id = @Id;
