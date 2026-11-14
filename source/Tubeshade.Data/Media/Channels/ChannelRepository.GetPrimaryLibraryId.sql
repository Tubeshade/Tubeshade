SELECT id
FROM media.libraries
INNER JOIN media.library_channels ON libraries.id = library_channels.library_id
WHERE library_channels.channel_id = @Id AND library_channels."primary" = true
  AND libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
