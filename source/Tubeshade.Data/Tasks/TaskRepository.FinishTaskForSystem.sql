INSERT INTO tasks.task_run_results (run_id, result, message)
VALUES (@taskRunId, @result, @message)
ON CONFLICT (run_id) DO NOTHING
RETURNING id;
