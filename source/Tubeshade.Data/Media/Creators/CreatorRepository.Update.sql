UPDATE media.creators
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    name = @Name
WHERE creators.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))
  AND creators.id = @Id;
