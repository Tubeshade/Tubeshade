INSERT INTO tasks.task_run_progress (run_id, value, target)
SELECT task_runs.id, @value, @target
FROM tasks.task_runs
WHERE task_runs.public_id = @taskRunId;
