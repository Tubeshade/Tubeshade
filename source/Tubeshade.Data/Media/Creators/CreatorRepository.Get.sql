SELECT id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       owner_id,
       name
FROM media.creators
WHERE creators.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
