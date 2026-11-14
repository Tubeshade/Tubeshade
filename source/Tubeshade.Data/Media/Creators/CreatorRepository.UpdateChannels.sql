WITH creator AS (SELECT creators.id
                 FROM media.creators
                 WHERE creators.public_id = @creatorId
                   AND creators.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @CreatorPermissions))),
    libs AS (SELECT libraries.id
             FROM media.libraries
             WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @LibraryPermissions))),
    accessible AS (SELECT channel_id
                   FROM media.library_channels
                   WHERE EXISTS(SELECT 1 FROM libs WHERE libs.id = library_id))

DELETE
FROM media.creator_channels
WHERE creator_id IN (SELECT creator.id FROM creator)
  AND EXISTS(SELECT 1 FROM accessible WHERE accessible.channel_id = creator_channels.channel_id);

WITH creator AS (SELECT creators.id
                 FROM media.creators
                 WHERE creators.public_id = @creatorId
                   AND creators.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @CreatorPermissions))),
    libs AS (SELECT libraries.id
             FROM media.libraries
             WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @LibraryPermissions))),
    accessible AS (SELECT channel_id
                   FROM media.library_channels
                   WHERE EXISTS(SELECT 1 FROM libs WHERE libs.id = library_id)),
    insertable AS (SELECT creator.id AS creator_id,
                          channels.id AS channel_id,
                          false AS "primary"
                   FROM creator,
                        media.channels
                   WHERE channels.public_id = ANY(@channelIds)
                     AND EXISTS(SELECT 1 from accessible WHERE accessible.channel_id = channels.id))

INSERT
INTO media.creator_channels (creator_id, channel_id, "primary")
SELECT *
FROM insertable;

WITH creator AS (SELECT creators.id
                 FROM media.creators
                 WHERE creators.public_id = @creatorId
                   AND creators.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @CreatorPermissions))),
    libs AS (SELECT libraries.id
             FROM media.libraries
             WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @LibraryPermissions))),
    accessible AS (SELECT channel_id
                   FROM media.library_channels
                   WHERE EXISTS(SELECT 1 FROM libs WHERE libs.id = library_id)),
    insertable AS (SELECT creator.id AS creator_id,
                          channels.id AS channel_id,
                          true AS "primary"
                   FROM creator,
                        media.channels
                   WHERE channels.public_id = @primaryChannelId)

INSERT
INTO media.creator_channels (creator_id, channel_id, "primary")
SELECT *
FROM insertable
WHERE EXISTS(SELECT 1 FROM accessible WHERE accessible.channel_id = insertable.channel_id);
