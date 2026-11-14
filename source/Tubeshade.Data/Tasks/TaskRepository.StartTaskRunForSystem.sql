UPDATE tasks.task_runs
SET state = 'running'
WHERE public_id = @taskRunId;
