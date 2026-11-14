INSERT INTO media.channel_images (channel_id, image_id)
SELECT channels.id, @id
FROM media.channels
WHERE channels.id = @channelId
  AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
