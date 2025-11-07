CREATE TYPE tasks.run_state AS ENUM ('queued', 'running', 'finished');

ALTER TABLE tasks.task_runs
    ADD COLUMN state tasks.run_state NULL;

UPDATE tasks.task_runs
SET state = 'finished'
WHERE id IN (SELECT run_id FROM tasks.task_run_results);

UPDATE tasks.task_runs
SET state = 'running'
WHERE NOT EXISTS(SELECT task_run_results.id FROM tasks.task_run_results WHERE run_id = task_runs.id);

ALTER TABLE tasks.task_runs
    ALTER COLUMN state SET NOT NULL;
