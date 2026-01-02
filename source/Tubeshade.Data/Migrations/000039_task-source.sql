CREATE TYPE tasks.task_source AS ENUM ('unknown', 'user', 'schedule', 'webhook');

ALTER TABLE tasks.task_runs
    ADD COLUMN source tasks.task_source DEFAULT 'unknown' NOT NULL;

ALTER TABLE tasks.task_runs
    ALTER COLUMN source DROP DEFAULT;
