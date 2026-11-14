SELECT pg_notify('cancel_task', task_runs.id::text)
FROM tasks.task_runs
     INNER JOIN tasks.tasks ON task_runs.task_id = tasks.id
WHERE (tasks.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))) AND task_runs.public_id = @taskRunId;
