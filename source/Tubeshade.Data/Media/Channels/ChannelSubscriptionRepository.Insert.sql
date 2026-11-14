INSERT INTO media.channel_subscriptions (id, created_by_user_id, modified_by_user_id, status, callback, topic, expires_at, verify_token, secret)
SELECT @Id, @CreatedByUserId, @ModifiedByUserId, @Status, @Callback, @Topic, @ExpiresAt, @VerifyToken, @Secret
WHERE @Id IN (SELECT channels.id
              FROM media.channels
              WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions)))
RETURNING id;
