UPDATE media.channel_subscriptions
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    status = @Status,
    callback = @Callback,
    topic = @Topic,
    expires_at = @ExpiresAt,
    verify_token = @VerifyToken,
    secret = @Secret
WHERE channel_subscriptions.id IN (SELECT channels.id FROM media.channels WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions)))
  AND channel_subscriptions.id = @Id;
