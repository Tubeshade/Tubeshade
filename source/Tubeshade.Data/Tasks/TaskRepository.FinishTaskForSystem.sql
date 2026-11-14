INSERT INTO tasks.task_run_results (run_id, result, message)
SELECT task_runs.id, @result, @message
FROM tasks.task_runs
WHERE task_runs.public_id = @taskRunId
ON CONFLICT (run_id) DO NOTHING
RETURNING id;
