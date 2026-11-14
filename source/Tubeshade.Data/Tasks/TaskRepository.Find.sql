SELECT id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       owner_id,
       type,
       user_id,
       library_id,
       channel_id,
       playlist_id,
       video_id,
       url,
       all_videos,
       payload
FROM tasks.tasks
WHERE tasks.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND tasks.id = @Id;
