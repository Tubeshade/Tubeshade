DELETE FROM media.creators
WHERE creators.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND creators.public_id = @Id;
