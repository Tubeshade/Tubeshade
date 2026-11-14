SELECT creator_channels.channel_id,
       creator_channels."primary"
FROM media.creator_channels
    INNER JOIN media.creators ON creators.id = creator_channels.creator_id
WHERE creator_channels.creator_id = @creatorId
  AND creators.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
