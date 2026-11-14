SELECT libraries.public_id
FROM media.libraries
    INNER JOIN media.library_channels ON libraries.id = library_channels.library_id
    INNER JOIN media.channels ON channels.id = library_channels.channel_id
WHERE channels.public_id = @Id AND library_channels."primary" = true
  AND libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
