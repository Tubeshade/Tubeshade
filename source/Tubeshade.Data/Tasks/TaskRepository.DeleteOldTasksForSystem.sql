WITH successful AS
    (
        SELECT tasks.id
        FROM tasks.tasks
        WHERE NOT EXISTS(SELECT 1
                         FROM tasks.task_runs
                             LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
                         WHERE task_id = tasks.id
                           AND (task_run_results.result IS NULL OR
                                task_run_results.result != 'successful' OR
                                task_run_results.created_at > CURRENT_TIMESTAMP - '1 month'::interval))
          AND EXISTS(SELECT 1 FROM tasks.task_runs WHERE task_id = tasks.id)
          AND NOT EXISTS(SELECT 1 FROM tasks.schedules WHERE task_id = tasks.id)
          AND NOT EXISTS(SELECT 1 FROM tasks.task_requests WHERE task_id = tasks.id)
    )

DELETE
FROM tasks.tasks
WHERE tasks.id IN (SELECT id FROM successful);
