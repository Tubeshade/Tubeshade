SELECT task_runs.public_id
FROM tasks.task_runs
    LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
WHERE task_run_results.id IS NULL;
