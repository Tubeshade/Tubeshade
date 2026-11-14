INSERT INTO media.channel_images (channel_id, image_id)
SELECT channels.id, image_files.id
FROM media.channels,
     media.image_files
WHERE channels.public_id = @channelId
  AND image_files.public_id = @id
  AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
