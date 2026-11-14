UPDATE tasks.tasks
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    type = @Type,
    user_id = @UserId,
    library_id = @LibraryId,
    channel_id = @ChannelId,
    playlist_id = @PlaylistId,
    video_id = @VideoId,
    url = @Url,
    all_videos = @AllVideos,
    payload = @Payload
WHERE tasks.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))
  AND tasks.id = @Id;
