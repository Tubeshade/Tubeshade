SELECT channels.public_id AS channel_id,
       creator_channels."primary"
FROM media.creator_channels
    INNER JOIN media.creators ON creators.id = creator_channels.creator_id
    INNER JOIN media.channels ON channels.id = creator_channels.channel_id
WHERE creators.public_id = @creatorId
  AND creators.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
