INSERT INTO tasks.task_requests (task_id, source)
SELECT tasks.id, @source::tasks.task_source
FROM tasks.tasks
WHERE (tasks.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))) AND tasks.public_id = @taskId
RETURNING id;
