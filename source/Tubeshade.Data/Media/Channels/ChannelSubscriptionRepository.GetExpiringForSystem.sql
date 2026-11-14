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
WHERE channel_subscriptions.expires_at IS NOT NULL
  AND (CURRENT_TIMESTAMP - channel_subscriptions.expires_at) >= '-2 days'::interval;
