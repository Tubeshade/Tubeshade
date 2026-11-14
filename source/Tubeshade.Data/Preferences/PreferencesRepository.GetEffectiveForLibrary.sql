SELECT library_p.id,
       library_p.public_id,
       library_p.created_at,
       library_p.created_by_user_id,
       library_p.modified_at,
       library_p.modified_by_user_id,
       library_p.playback_speed,
       library_p.videos_count,
       library_p.live_streams_count,
       library_p.shorts_count,
       library_p.player_client,
       library_p.download_videos,
       library_p.download_method,
       library_p.formats
FROM media.libraries
         LEFT OUTER JOIN media.library_preferences ON libraries.id = library_preferences.library_id
         LEFT OUTER JOIN media.preferences library_p ON library_preferences.preference_id = library_p.id
WHERE libraries.public_id = @libraryId
  AND libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
