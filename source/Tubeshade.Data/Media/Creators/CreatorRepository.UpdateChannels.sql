WITH libs AS (SELECT libraries.id
              FROM media.libraries
              WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @LibraryPermissions))),
    accessible AS (SELECT channel_id
                   FROM media.library_channels
                   WHERE EXISTS(SELECT 1 FROM libs WHERE libs.id = library_id))

DELETE
FROM media.creator_channels
WHERE creator_id = @creatorId
  AND creator_id IN (SELECT creators.id FROM media.creators WHERE creators.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @CreatorPermissions)))
  AND EXISTS(SELECT 1 FROM accessible WHERE accessible.channel_id = creator_channels.channel_id);

WITH libs AS (SELECT libraries.id
              FROM media.libraries
              WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @LibraryPermissions))),
    accessible AS (SELECT channel_id
                   FROM media.library_channels
                   WHERE EXISTS(SELECT 1 FROM libs WHERE libs.id = library_id)),
    insertable AS (SELECT @creatorId AS creator_id,
                          channel_id,
                          false AS "primary"
                   FROM UNNEST(@channelIds) AS t(channel_id)
                   WHERE EXISTS(SELECT 1 from accessible WHERE accessible.channel_id = t.channel_id)
                     AND @creatorId IN (SELECT creators.id FROM media.creators WHERE creators.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @CreatorPermissions))))

INSERT
INTO media.creator_channels (creator_id, channel_id, "primary")
SELECT *
FROM insertable;

WITH libs AS (SELECT libraries.id
              FROM media.libraries
              WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @LibraryPermissions))),
    accessible AS (SELECT channel_id
                   FROM media.library_channels
                   WHERE EXISTS(SELECT 1 FROM libs WHERE libs.id = library_id)),
    insertable AS (SELECT @creatorId AS creator_id,
                          @primaryChannelId AS channel_id,
                          true AS "primary")

INSERT
INTO media.creator_channels (creator_id, channel_id, "primary")
SELECT *
FROM insertable
WHERE EXISTS(SELECT 1 FROM accessible WHERE accessible.channel_id = insertable.channel_id)
  AND @creatorId IN (SELECT creators.id FROM media.creators WHERE creators.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @CreatorPermissions)));
