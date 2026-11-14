SELECT videos.public_id AS id,
       videos.external_id
FROM media.videos
WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND (videos.external_id = ANY(@externalIds));
