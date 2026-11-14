INSERT INTO media.library_channels (library_id, channel_id, "primary")
SELECT libraries.id, channels.id, true
FROM media.libraries,
     media.channels
WHERE libraries.id = @libraryId
  AND libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @LibraryPermissions))
  AND channels.id = @channelId
  AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @ChannelPermissions));
