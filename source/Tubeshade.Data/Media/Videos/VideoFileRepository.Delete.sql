DELETE FROM media.video_files
WHERE video_files.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND video_files.public_id = @Id;
