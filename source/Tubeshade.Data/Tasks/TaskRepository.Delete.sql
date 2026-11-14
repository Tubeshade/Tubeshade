DELETE FROM tasks.tasks
WHERE tasks.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND tasks.public_id = @Id;
