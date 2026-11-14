DELETE FROM media.channels
WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND channels.public_id = @Id;
