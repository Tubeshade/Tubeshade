DELETE FROM media.channel_subscriptions
WHERE channel_subscriptions.id IN (SELECT channels.id FROM media.channels WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
  AND channel_subscriptions.id = @Id;
