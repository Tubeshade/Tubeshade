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
FROM media.preferences
WHERE preferences.id IN (SELECT library_preferences.preference_id
                         FROM media.library_preferences
                         WHERE library_preferences.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
                         UNION ALL
                         SELECT channel_preferences.preference_id
                         FROM media.channel_preferences
                             INNER JOIN media.channels ON channels.id = channel_preferences.channel_id
                         WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
  AND preferences.id = @Id;
