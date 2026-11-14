INSERT INTO media.channel_preferences (channel_id, preference_id)
SELECT channels.id, preferences.id
FROM media.channels,
     media.preferences
WHERE channels.public_id = @channelId
  AND preferences.public_id = @preferenceId
  AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
