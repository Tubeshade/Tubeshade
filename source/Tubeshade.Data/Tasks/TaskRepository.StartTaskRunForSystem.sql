UPDATE tasks.task_runs
SET state = 'running'
WHERE id = @taskRunId;
