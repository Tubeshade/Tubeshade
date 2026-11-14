INSERT INTO media.libraries (created_by_user_id, modified_by_user_id, owner_id, name, storage_path, subscriptions_schedule_id)
SELECT @CreatedByUserId, @ModifiedByUserId, @OwnerId, @Name, @StoragePath, schedules.id
FROM tasks.schedules
WHERE schedules.public_id = @SubscriptionsScheduleId
  AND @OwnerId IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING public_id;
