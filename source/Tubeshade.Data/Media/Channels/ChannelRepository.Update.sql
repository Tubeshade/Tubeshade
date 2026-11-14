UPDATE media.channels
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    name = @Name,
    storage_path = @StoragePath,
    external_id = @ExternalId,
    subscribed_at = @SubscribedAt,
    subscriber_count = @SubscriberCount
WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))
  AND channels.id = @Id;
