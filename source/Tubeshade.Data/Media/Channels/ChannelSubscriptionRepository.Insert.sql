WITH inserted AS
    (INSERT INTO media.channel_subscriptions (channel_id, created_by_user_id, modified_by_user_id, status, callback, topic, expires_at, verify_token, secret)
     SELECT channels.id, @CreatedByUserId, @ModifiedByUserId, @Status, @Callback, @Topic, @ExpiresAt, @VerifyToken, @Secret
     FROM media.channels
     WHERE channels.public_id = @PublicId
       AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
     RETURNING channel_id)

SELECT channels.public_id
FROM inserted
    INNER JOIN media.channels ON channels.id = inserted.channel_id;
