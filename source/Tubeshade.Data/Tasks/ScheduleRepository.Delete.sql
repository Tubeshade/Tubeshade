DELETE FROM tasks.schedules
WHERE schedules.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND schedules.id = @Id;
