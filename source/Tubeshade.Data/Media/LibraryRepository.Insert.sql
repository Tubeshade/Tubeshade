INSERT INTO media.libraries (created_by_user_id, modified_by_user_id, owner_id, name, storage_path, subscriptions_schedule_id)
SELECT @CreatedByUserId, @ModifiedByUserId, @OwnerId, @Name, @StoragePath, @SubscriptionsScheduleId
WHERE @OwnerId IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING id;
