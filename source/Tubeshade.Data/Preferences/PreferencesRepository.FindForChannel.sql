SELECT preferences.id,
       preferences.created_at,
       preferences.created_by_user_id,
       preferences.modified_at,
       preferences.modified_by_user_id,
       preferences.playback_speed,
       preferences.videos_count,
       preferences.live_streams_count,
       preferences.shorts_count,
       preferences.player_client,
       preferences.download_videos,
       preferences.download_method,
       preferences.formats
FROM media.channels
   INNER JOIN media.channel_preferences ON channels.id = channel_preferences.channel_id
   INNER JOIN media.preferences ON channel_preferences.preference_id = preferences.id
WHERE channels.id = @channelId
  AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
