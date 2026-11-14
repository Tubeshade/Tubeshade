DELETE FROM media.libraries
WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND libraries.id = @Id;
