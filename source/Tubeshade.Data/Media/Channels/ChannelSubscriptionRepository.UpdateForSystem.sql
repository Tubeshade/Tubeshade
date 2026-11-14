UPDATE media.channel_subscriptions
SET modified_at = CURRENT_TIMESTAMP,
    status = @Status,
    callback = @Callback,
    topic = @Topic,
    expires_at = @ExpiresAt,
    verify_token = @VerifyToken,
    secret = @Secret
WHERE channel_subscriptions.channel_id = @Id;
