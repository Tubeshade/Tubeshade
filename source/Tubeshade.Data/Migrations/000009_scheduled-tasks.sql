CREATE TABLE tasks.schedules
(
    id                  uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id  uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    owner_id            uuid                                   NOT NULL REFERENCES identity.owners (id) NOT DEFERRABLE,

    task_id             uuid                                   NOT NULL REFERENCES tasks.tasks (id) NOT DEFERRABLE,
    cron_expression     text                                   NOT NULL,
    time_zone_id        text                                   NOT NULL
);

ALTER TABLE media.libraries
    ADD COLUMN subscriptions_schedule_id uuid NULL REFERENCES tasks.schedules (id) NOT DEFERRABLE;

-- In order to set the subscriptions schedule id, we first need to:
WITH system AS (SELECT id, name, normalized_name FROM identity.users WHERE normalized_name = 'SYSTEM'),

-- 1) Create a 'scan_subscriptions' task for each library
     tasks AS (INSERT INTO tasks.tasks (created_by_user_id, modified_by_user_id, owner_id, type, payload)
         SELECT system.id,
                system.id,
                system.id,
                'scan_subscriptions',
                format('{"userId": "%s", "libraryId": "%s"}', system.id, libraries.id)
         FROM media.libraries,
              system
         RETURNING id, created_by_user_id AS system_user_id, (payload::json ->> 'libraryId')::uuid AS library_id),

-- 2) Create a schedule with that task for each library
     schedules AS (
         INSERT INTO tasks.schedules (created_by_user_id, modified_by_user_id, owner_id, task_id, cron_expression, time_zone_id)
             SELECT tasks.system_user_id, tasks.system_user_id, libraries.owner_id, tasks.id, '0 5 * * *', 'Etc/UTC'
             FROM media.libraries AS libraries
                      INNER JOIN tasks ON tasks.library_id = libraries.id
             RETURNING id AS schedule_id, created_by_user_id AS system_user_id)

UPDATE media.libraries
SET modified_at               = CURRENT_TIMESTAMP,
    modified_by_user_id       = schedules.system_user_id,
    subscriptions_schedule_id = schedules.schedule_id
FROM schedules;

ALTER TABLE media.libraries
    ALTER COLUMN subscriptions_schedule_id SET NOT NULL;
