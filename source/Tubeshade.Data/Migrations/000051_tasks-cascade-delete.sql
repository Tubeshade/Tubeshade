ALTER TABLE tasks.task_runs
    DROP CONSTRAINT task_runs_task_id_fkey;

ALTER TABLE tasks.task_runs
    ADD FOREIGN KEY (task_id) REFERENCES tasks.tasks (id) ON DELETE CASCADE;


ALTER TABLE tasks.task_run_progress
    DROP CONSTRAINT task_run_progress_run_id_fkey;

ALTER TABLE tasks.task_run_progress
    ADD FOREIGN KEY (run_id) REFERENCES tasks.task_runs (id) ON DELETE CASCADE;


ALTER TABLE tasks.task_run_results
    DROP CONSTRAINT task_run_results_run_id_fkey;

ALTER TABLE tasks.task_run_results
    ADD FOREIGN KEY (run_id) REFERENCES tasks.task_runs (id) ON DELETE CASCADE;


WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM'),
     delete_task AS (
         INSERT
         INTO tasks.tasks (created_by_user_id, modified_by_user_id, owner_id, type, user_id)
         SELECT id, id, id, 'delete_tasks', id
         FROM system
         RETURNING id)

INSERT
INTO tasks.schedules (created_by_user_id, modified_by_user_id, task_id, cron_expression, time_zone_id, owner_id)
SELECT system.id, system.id, delete_task.id, '@daily', 'Etc/UTC', system.id
FROM system,
     delete_task;
