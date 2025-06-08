DROP SCHEMA IF EXISTS tasks CASCADE;
CREATE SCHEMA tasks;

CREATE TYPE tasks.task_type AS ENUM ('index_video', 'download_video');
CREATE TYPE tasks.task_result AS ENUM ('successful', 'failed', 'cancelled');

CREATE TABLE tasks.tasks
(
    id                  uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id  uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    owner_id            uuid                                   NOT NULL REFERENCES identity.owners (id) NOT DEFERRABLE,
    type                tasks.task_type                        NOT NULL,
    payload             text                                   NOT NULL
);

CREATE OR REPLACE FUNCTION notify_on_task_created()
    RETURNS TRIGGER AS
$$
BEGIN
    PERFORM pg_notify('task_created', NEW.id::text);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tasks_created
    AFTER INSERT
    ON tasks.tasks
    REFERENCING NEW TABLE AS inserted
    FOR EACH ROW
EXECUTE FUNCTION notify_on_task_created();

CREATE TABLE tasks.task_runs
(
    id         uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    task_id    uuid                                   NOT NULL REFERENCES tasks.tasks (id) NOT DEFERRABLE
);

CREATE TABLE tasks.task_run_progress
(
    run_id      uuid                                  NOT NULL PRIMARY KEY REFERENCES tasks.task_runs (id),
    modified_at timestamptz DEFAULT CURRENT_TIMESTAMP NOT NULL,
    value       decimal                               NOT NULL,
    target      decimal                               NOT NULL
);

CREATE TABLE tasks.task_run_results
(
    id         uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,

    run_id     uuid                                   NOT NULL REFERENCES tasks.task_runs (id) NOT DEFERRABLE,
    result     tasks.task_result                      NOT NULL,
    message    text                                   NULL
);

CREATE INDEX ON tasks.task_run_results (run_id, created_at ASC);

CREATE VIEW tasks.queued_tasks AS
SELECT tasks.id AS TaskId
FROM tasks.tasks
         LEFT OUTER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
         LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
WHERE task_runs.id IS NULL
ORDER BY tasks.created_at;

CREATE VIEW tasks.running_tasks AS
SELECT tasks.id                                                 AS TaskId,
       tasks.type                                               AS TaskType,
       task_runs.id                                             AS TaskRunId,
       task_run_progress.value / task_run_progress.target * 100 AS Progress
FROM tasks.tasks
         INNER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
         LEFT OUTER JOIN tasks.task_run_progress ON task_runs.id = task_run_progress.run_id
         LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
WHERE task_run_results.id IS NULL
ORDER BY task_runs.created_at;

CREATE VIEW tasks.completed_tasks AS
SELECT tasks.id            AS TaskId,
       task_runs.id        AS TaskRunId,
       task_run_results.id AS TaskRunResult
FROM tasks.tasks
         INNER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
         INNER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
ORDER BY task_run_results.created_at;
