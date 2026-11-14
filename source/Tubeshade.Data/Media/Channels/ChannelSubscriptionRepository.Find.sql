SELECT channel_subscriptions.channel_id AS id,
       channels.public_id,
       channel_subscriptions.created_at,
       channel_subscriptions.created_by_user_id,
       channel_subscriptions.modified_at,
       channel_subscriptions.modified_by_user_id,
       channel_subscriptions.status,
       channel_subscriptions.callback,
       channel_subscriptions.topic,
       channel_subscriptions.expires_at,
       channel_subscriptions.verify_token,
       channel_subscriptions.secret
FROM media.channel_subscriptions
    INNER JOIN media.channels ON channels.id = channel_subscriptions.channel_id
WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND channels.public_id = @Id;
