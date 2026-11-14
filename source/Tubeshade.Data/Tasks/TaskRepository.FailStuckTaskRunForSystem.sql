INSERT INTO tasks.task_run_results (run_id, result, message)
VALUES (@taskRunId, @result, 'Failed because server was stopped unexpectedly')
ON CONFLICT (run_id) DO NOTHING;
