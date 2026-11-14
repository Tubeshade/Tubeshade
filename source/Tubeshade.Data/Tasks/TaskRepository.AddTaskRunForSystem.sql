INSERT INTO tasks.task_runs (public_id, task_id, state, source)
SELECT @taskRunId, tasks.id, 'queued', @source
FROM tasks.tasks
WHERE tasks.public_id = @taskId
RETURNING id;
