DELETE
FROM tasks.task_runs
    USING tasks.tasks, tasks.task_run_results
WHERE task_runs.task_id = tasks.id
  AND task_run_results.run_id = task_runs.id
  AND tasks.type = 'reindex_videos'
  AND (task_run_results.created_at <= CURRENT_TIMESTAMP - '1 month'::interval OR
       (task_run_results.result = 'successful' AND task_run_results.created_at <= CURRENT_TIMESTAMP - '1 day'::interval));
