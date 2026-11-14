SELECT CASE WHEN task_run_results.result = 'successful' THEN NULL ELSE task_run_progress.value END
FROM tasks.task_runs AS current_run
         INNER JOIN tasks.task_runs ON task_runs.task_id = current_run.task_id AND task_runs.created_at < current_run.created_at
         INNER JOIN tasks.task_run_progress ON task_runs.id = task_run_progress.run_id
         LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
WHERE current_run.public_id = @taskRunId
ORDER BY task_runs.created_at DESC
LIMIT 1;
