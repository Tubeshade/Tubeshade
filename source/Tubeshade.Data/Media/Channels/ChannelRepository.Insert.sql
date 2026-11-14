INSERT INTO media.channels (created_by_user_id, modified_by_user_id, owner_id, name, storage_path, external_id, subscribed_at, subscriber_count, external_url, availability)
SELECT @CreatedByUserId, @ModifiedByUserId, @OwnerId, @Name, @StoragePath, @ExternalId, @SubscribedAt, @SubscriberCount, @ExternalUrl, @Availability
WHERE @OwnerId IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING public_id;
