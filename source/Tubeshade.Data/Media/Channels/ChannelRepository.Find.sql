SELECT id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       owner_id,
       name,
       storage_path,
       external_id,
       subscribed_at,
       subscriber_count,
       external_url,
       availability
FROM media.channels
WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND channels.id = @Id;
