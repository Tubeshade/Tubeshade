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
WHERE channel_subscriptions.id IN (SELECT channels.id FROM media.channels WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
  AND channel_subscriptions.id = @Id;
