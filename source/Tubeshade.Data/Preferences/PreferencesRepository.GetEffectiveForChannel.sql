SELECT COALESCE(channel_p.id, library_p.id) AS Id,
       COALESCE(channel_p.created_at, library_p.created_at) AS CreatedAt,
       COALESCE(channel_p.created_by_user_id, library_p.created_by_user_id) AS CreatedByUserId,
       COALESCE(channel_p.modified_at, library_p.modified_at) AS ModifiedAt,
       COALESCE(channel_p.modified_by_user_id, library_p.modified_by_user_id) AS ModifiedByUserId,
       COALESCE(channel_p.playback_speed, library_p.playback_speed) AS PlaybackSpeed,
       COALESCE(channel_p.videos_count, library_p.videos_count) AS VideosCount,
       COALESCE(channel_p.live_streams_count, library_p.live_streams_count) AS LiveStreamsCount,
       COALESCE(channel_p.shorts_count, library_p.shorts_count) AS ShortsCount,
       COALESCE(channel_p.player_client, library_p.player_client) AS PlayerClient,
       COALESCE(channel_p.download_videos, library_p.download_videos) AS DownloadVideos,
       COALESCE(channel_p.download_method, library_p.download_method) AS DownloadMethod,
       COALESCE(channel_p.formats, library_p.formats) AS Formats
FROM media.channels
         LEFT OUTER JOIN media.channel_preferences ON channels.id = channel_preferences.channel_id
         LEFT OUTER JOIN media.preferences channel_p ON channel_preferences.preference_id = channel_p.id

         INNER JOIN media.libraries ON libraries.id = @libraryId
         LEFT OUTER JOIN media.library_preferences ON libraries.id = library_preferences.library_id
         LEFT OUTER JOIN media.preferences library_p ON library_preferences.preference_id = library_p.id
WHERE channels.id = @channelId AND COALESCE(channel_p.id, library_p.id) IS NOT NULL
  AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
