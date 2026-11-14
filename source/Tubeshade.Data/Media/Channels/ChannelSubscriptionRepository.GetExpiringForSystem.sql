SELECT id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       status,
       callback,
       topic,
       expires_at,
       verify_token,
       secret
FROM media.channel_subscriptions
WHERE expires_at IS NOT NULL AND (CURRENT_TIMESTAMP - expires_at) >= '-2 days'::interval;
