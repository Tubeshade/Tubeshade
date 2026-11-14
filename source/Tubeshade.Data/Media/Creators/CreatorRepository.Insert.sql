INSERT INTO media.creators (created_by_user_id, modified_by_user_id, owner_id, name)
SELECT @CreatedByUserId, @ModifiedByUserId, @OwnerId, @Name
WHERE @OwnerId IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING id;
