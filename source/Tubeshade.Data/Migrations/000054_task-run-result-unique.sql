-- keep only the first result of each task run
DELETE
FROM tasks.task_run_results
WHERE id IN (SELECT id
             FROM (SELECT id,
                          row_number() OVER (PARTITION BY run_id ORDER BY created_at, id) AS row_number
                   FROM tasks.task_run_results) AS results
             WHERE row_number > 1);

ALTER TABLE tasks.task_run_results
    ADD UNIQUE (run_id);

-- covered by the unique constraint
DROP INDEX IF EXISTS tasks.task_run_results_run_id_created_at_idx;
