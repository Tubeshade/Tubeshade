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
FROM media.libraries
   INNER JOIN media.library_preferences ON libraries.id = library_preferences.library_id
   INNER JOIN media.preferences ON library_preferences.preference_id = preferences.id
WHERE libraries.id = @libraryId
  AND libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
