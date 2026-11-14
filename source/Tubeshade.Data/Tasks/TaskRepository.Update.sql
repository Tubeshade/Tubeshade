UPDATE tasks.tasks
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    type = @Type,
    user_id = @UserId,
    library_id = (SELECT libraries.id FROM media.libraries WHERE libraries.public_id = @LibraryId),
    channel_id = (SELECT channels.id FROM media.channels WHERE channels.public_id = @ChannelId),
    playlist_id = (SELECT playlists.id FROM media.playlists WHERE playlists.public_id = @PlaylistId),
    video_id = (SELECT videos.id FROM media.videos WHERE videos.public_id = @VideoId),
    url = @Url,
    all_videos = @AllVideos,
    payload = @Payload
WHERE tasks.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))
  AND tasks.public_id = @PublicId;
