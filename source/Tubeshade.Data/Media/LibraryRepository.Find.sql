SELECT id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       owner_id,
       name,
       storage_path,
       subscriptions_schedule_id
FROM media.libraries
WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND libraries.id = @Id;
