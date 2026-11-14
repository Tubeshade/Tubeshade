UPDATE tasks.task_runs
SET state = 'finished'
WHERE public_id = @taskRunId;

-- The internal id is what blocked task runs wait for, and the public id is for logging
SELECT pg_notify('task_run_finished', task_runs.id::text || ':' || task_runs.public_id::text)
FROM tasks.task_runs
WHERE public_id = @taskRunId;
