-- Requests to run a task, stored until a worker adds a task run for them,
-- so that they can be redelivered if the notification is lost or the worker fails before committing.
CREATE TABLE tasks.task_requests
(
    id         uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,

    task_id    uuid                                   NOT NULL REFERENCES tasks.tasks (id) ON DELETE CASCADE,
    source     tasks.task_source                      NOT NULL
);

CREATE INDEX ON tasks.task_requests (task_id);

CREATE FUNCTION tasks.notify_on_task_requested()
    RETURNS TRIGGER AS
$$
BEGIN
    PERFORM pg_notify('task_requested', NEW.id::text);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER task_requests_created
    AFTER INSERT
    ON tasks.task_requests
    FOR EACH ROW
EXECUTE FUNCTION tasks.notify_on_task_requested();
