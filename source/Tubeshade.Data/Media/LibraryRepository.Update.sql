UPDATE media.libraries
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    name = @Name,
    storage_path = @StoragePath,
    subscriptions_schedule_id = @SubscriptionsScheduleId
WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))
  AND libraries.id = @Id;
