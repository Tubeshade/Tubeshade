UPDATE media.preferences
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    playback_speed = @PlaybackSpeed,
    videos_count = @VideosCount,
    live_streams_count = @LiveStreamsCount,
    shorts_count = @ShortsCount,
    player_client = @PlayerClient,
    download_videos = @DownloadVideos,
    download_method = @DownloadMethod,
    formats = @Formats
WHERE preferences.id IN (SELECT library_preferences.preference_id
                         FROM media.library_preferences
                             INNER JOIN media.libraries ON libraries.id = library_preferences.library_id
                         WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))
                         UNION ALL
                         SELECT channel_preferences.preference_id
                         FROM media.channel_preferences
                             INNER JOIN media.channels ON channels.id = channel_preferences.channel_id
                         WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions)))
  AND preferences.public_id = @PublicId;
