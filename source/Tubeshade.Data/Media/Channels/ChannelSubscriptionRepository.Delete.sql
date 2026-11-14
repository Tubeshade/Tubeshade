DELETE FROM media.channel_subscriptions
    USING media.channels
WHERE channels.id = channel_subscriptions.channel_id
  AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND channels.public_id = @Id;
