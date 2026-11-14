SELECT task_runs.id AS run_id, tasks.public_id AS task_id, task_runs.source
FROM tasks.task_runs
    INNER JOIN tasks.tasks ON tasks.id = task_runs.task_id
WHERE task_runs.public_id = @taskRunId;
