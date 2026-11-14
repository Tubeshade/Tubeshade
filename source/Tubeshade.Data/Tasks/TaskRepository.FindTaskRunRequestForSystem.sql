SELECT task_id, source
FROM tasks.task_runs
WHERE id = @taskRunId;
