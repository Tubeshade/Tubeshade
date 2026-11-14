UPDATE tasks.task_runs
SET state = 'finished'
WHERE id = @taskRunId;

SELECT pg_notify('task_run_finished', @taskRunId::text);
