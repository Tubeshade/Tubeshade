INSERT INTO media.channel_preferences (channel_id, preference_id)
SELECT channels.id, @preferenceId
FROM media.channels
WHERE channels.id = @channelId
  AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
