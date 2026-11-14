UPDATE media.channel_subscriptions
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    status = @Status,
    callback = @Callback,
    topic = @Topic,
    expires_at = @ExpiresAt,
    verify_token = @VerifyToken,
    secret = @Secret
FROM media.channels
WHERE channels.id = channel_subscriptions.channel_id
  AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))
  AND channels.public_id = @PublicId;
