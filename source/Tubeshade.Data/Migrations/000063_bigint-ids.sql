-- Every table gets a bigint key. Tables whose ids are used outside the database (routes, API, storage paths,
-- webhook callbacks, the browser extension) keep their uuid as public_id; internal tables lose their uuid.

DROP VIEW tasks.completed_tasks;
DROP VIEW tasks.queued_tasks;
DROP VIEW tasks.running_tasks;
DROP FUNCTION access.accessible_owners(uuid, integer);

ALTER TABLE access.owners RENAME COLUMN id TO public_id;
ALTER TABLE access.owners ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE identity.claims RENAME COLUMN id TO public_id;
ALTER TABLE identity.claims ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE media.channels RENAME COLUMN id TO public_id;
ALTER TABLE media.channels ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE media.creators RENAME COLUMN id TO public_id;
ALTER TABLE media.creators ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE media.image_files RENAME COLUMN id TO public_id;
ALTER TABLE media.image_files ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE media.libraries RENAME COLUMN id TO public_id;
ALTER TABLE media.libraries ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE media.playlists RENAME COLUMN id TO public_id;
ALTER TABLE media.playlists ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE media.preferences RENAME COLUMN id TO public_id;
ALTER TABLE media.preferences ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE media.sponsorblock_segments RENAME COLUMN id TO public_id;
ALTER TABLE media.sponsorblock_segments ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE media.track_files RENAME COLUMN id TO public_id;
ALTER TABLE media.track_files ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE media.video_files RENAME COLUMN id TO public_id;
ALTER TABLE media.video_files ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE media.videos RENAME COLUMN id TO public_id;
ALTER TABLE media.videos ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE tasks.schedules RENAME COLUMN id TO public_id;
ALTER TABLE tasks.schedules ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE tasks.tasks RENAME COLUMN id TO public_id;
ALTER TABLE tasks.tasks ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

ALTER TABLE tasks.task_runs RENAME COLUMN id TO public_id;
ALTER TABLE tasks.task_runs ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY;

-- Users share the id of their principal, created with the same public id in 000062_principals.sql.
-- The public id is kept only on the principal, once nothing references users through it.
ALTER TABLE identity.users RENAME COLUMN id TO public_id;
ALTER TABLE identity.users ADD COLUMN id bigint;

UPDATE identity.users
SET id = principals.id
FROM access.principals
WHERE principals.public_id = users.public_id;

ALTER TABLE identity.users ALTER COLUMN id SET NOT NULL;

ALTER TABLE media.video_plays DROP COLUMN id;
ALTER TABLE media.video_plays ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY;

ALTER TABLE tasks.task_run_results DROP COLUMN id;
ALTER TABLE tasks.task_run_results ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY;

ALTER TABLE tasks.task_requests DROP COLUMN id;
ALTER TABLE tasks.task_requests ADD COLUMN id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY;

-- Replaces a uuid reference with a bigint one, resolved through the referenced table's public_id.
-- The foreign key is added later, once the referenced table's primary key is on the new id.
-- Dropping the old column also drops the indexes and composite primary keys that include it; they are recreated below.
CREATE TEMPORARY TABLE pending_references
(
    source      regclass NOT NULL,
    column_name name     NOT NULL,
    target      regclass NOT NULL,
    on_delete   text     NOT NULL
) ON COMMIT DROP;

CREATE FUNCTION pg_temp.reference_by_id(source regclass, old_column name, new_column name, target regclass, on_delete text)
    RETURNS void
    LANGUAGE plpgsql
AS
$$
DECLARE
    not_null boolean;
    temporary_column name := new_column || '_bigint';
BEGIN
    SELECT attnotnull
    INTO STRICT not_null
    FROM pg_attribute
    WHERE attrelid = source
      AND attname = old_column
      AND NOT attisdropped;

    EXECUTE format('ALTER TABLE %s ADD COLUMN %I bigint', source, temporary_column);
    EXECUTE format('UPDATE %s AS source SET %I = target.id FROM %s AS target WHERE target.public_id = source.%I',
                   source, temporary_column, target, old_column);
    EXECUTE format('ALTER TABLE %s DROP COLUMN %I', source, old_column);
    EXECUTE format('ALTER TABLE %s RENAME COLUMN %I TO %I', source, temporary_column, new_column);

    IF not_null THEN
        EXECUTE format('ALTER TABLE %s ALTER COLUMN %I SET NOT NULL', source, new_column);
    END IF;

    INSERT INTO pending_references VALUES (source, new_column, target, on_delete);
END
$$;

SELECT pg_temp.reference_by_id('access.grants', 'principal_id', 'principal_id', 'access.principals', 'CASCADE');
SELECT pg_temp.reference_by_id('access.grants', 'owner_id', 'owner_id', 'access.owners', 'CASCADE');
SELECT pg_temp.reference_by_id('access.grants', 'granted_by', 'granted_by', 'access.principals', 'NO ACTION');

SELECT pg_temp.reference_by_id('access.owners', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('access.owners', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('access.owners', 'personal_for', 'personal_for', 'identity.users', 'CASCADE');

SELECT pg_temp.reference_by_id('identity.claims', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('identity.claims', 'user_id', 'user_id', 'identity.users', 'NO ACTION');

SELECT pg_temp.reference_by_id('identity.user_logins', 'user_id', 'user_id', 'identity.users', 'NO ACTION');

SELECT pg_temp.reference_by_id('identity.users', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('identity.users', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.channel_images', 'channel_id', 'channel_id', 'media.channels', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.channel_images', 'image_id', 'image_id', 'media.image_files', 'CASCADE');

SELECT pg_temp.reference_by_id('media.channel_preferences', 'channel_id', 'channel_id', 'media.channels', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.channel_preferences', 'preference_id', 'preference_id', 'media.preferences', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.channel_subscriptions', 'id', 'channel_id', 'media.channels', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.channel_subscriptions', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.channel_subscriptions', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.channels', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.channels', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.channels', 'owner_id', 'owner_id', 'access.owners', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.creator_channels', 'creator_id', 'creator_id', 'media.creators', 'CASCADE');
SELECT pg_temp.reference_by_id('media.creator_channels', 'channel_id', 'channel_id', 'media.channels', 'CASCADE');

SELECT pg_temp.reference_by_id('media.creators', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.creators', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.creators', 'owner_id', 'owner_id', 'access.owners', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.image_files', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.image_files', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.libraries', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.libraries', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.libraries', 'owner_id', 'owner_id', 'access.owners', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.libraries', 'subscriptions_schedule_id', 'subscriptions_schedule_id', 'tasks.schedules', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.library_channels', 'library_id', 'library_id', 'media.libraries', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.library_channels', 'channel_id', 'channel_id', 'media.channels', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.library_external_cookies', 'id', 'library_id', 'media.libraries', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.library_external_cookies', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.library_external_cookies', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.library_preferences', 'library_id', 'library_id', 'media.libraries', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.library_preferences', 'preference_id', 'preference_id', 'media.preferences', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.playlist_images', 'playlist_id', 'playlist_id', 'media.playlists', 'CASCADE');
SELECT pg_temp.reference_by_id('media.playlist_images', 'image_id', 'image_id', 'media.image_files', 'CASCADE');

SELECT pg_temp.reference_by_id('media.playlist_videos', 'playlist_id', 'playlist_id', 'media.playlists', 'CASCADE');
SELECT pg_temp.reference_by_id('media.playlist_videos', 'video_id', 'video_id', 'media.videos', 'CASCADE');

SELECT pg_temp.reference_by_id('media.playlists', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.playlists', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.playlists', 'library_id', 'library_id', 'media.libraries', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.preferences', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.preferences', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.sponsorblock_segments', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.sponsorblock_segments', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.sponsorblock_segments', 'video_id', 'video_id', 'media.videos', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.track_files', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.track_files', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.track_files', 'video_id', 'video_id', 'media.videos', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.video_files', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.video_files', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.video_files', 'owner_id', 'owner_id', 'access.owners', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.video_files', 'video_id', 'video_id', 'media.videos', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.video_files', 'downloaded_by_user_id', 'downloaded_by_user_id', 'access.principals', 'NO ACTION');

SELECT pg_temp.reference_by_id('media.video_files_downloading', 'file_id', 'file_id', 'media.video_files', 'CASCADE');
SELECT pg_temp.reference_by_id('media.video_files_downloading', 'task_run_id', 'task_run_id', 'tasks.task_runs', 'CASCADE');

SELECT pg_temp.reference_by_id('media.video_images', 'video_id', 'video_id', 'media.videos', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.video_images', 'image_id', 'image_id', 'media.image_files', 'CASCADE');

SELECT pg_temp.reference_by_id('media.video_plays', 'video_id', 'video_id', 'media.videos', 'CASCADE');
SELECT pg_temp.reference_by_id('media.video_plays', 'user_id', 'user_id', 'identity.users', 'CASCADE');

SELECT pg_temp.reference_by_id('media.video_viewed_by_users', 'video_id', 'video_id', 'media.videos', 'CASCADE');
SELECT pg_temp.reference_by_id('media.video_viewed_by_users', 'user_id', 'user_id', 'identity.users', 'CASCADE');

SELECT pg_temp.reference_by_id('media.videos', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.videos', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.videos', 'owner_id', 'owner_id', 'access.owners', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.videos', 'channel_id', 'channel_id', 'media.channels', 'NO ACTION');
SELECT pg_temp.reference_by_id('media.videos', 'ignored_by_user_id', 'ignored_by_user_id', 'access.principals', 'NO ACTION');

SELECT pg_temp.reference_by_id('tasks.schedules', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('tasks.schedules', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('tasks.schedules', 'owner_id', 'owner_id', 'access.owners', 'NO ACTION');
SELECT pg_temp.reference_by_id('tasks.schedules', 'task_id', 'task_id', 'tasks.tasks', 'NO ACTION');

SELECT pg_temp.reference_by_id('tasks.task_requests', 'task_id', 'task_id', 'tasks.tasks', 'CASCADE');

SELECT pg_temp.reference_by_id('tasks.task_run_progress', 'run_id', 'run_id', 'tasks.task_runs', 'CASCADE');

SELECT pg_temp.reference_by_id('tasks.task_run_results', 'run_id', 'run_id', 'tasks.task_runs', 'CASCADE');

SELECT pg_temp.reference_by_id('tasks.task_runs', 'task_id', 'task_id', 'tasks.tasks', 'CASCADE');

SELECT pg_temp.reference_by_id('tasks.tasks', 'created_by_user_id', 'created_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('tasks.tasks', 'modified_by_user_id', 'modified_by_user_id', 'access.principals', 'NO ACTION');
SELECT pg_temp.reference_by_id('tasks.tasks', 'owner_id', 'owner_id', 'access.owners', 'NO ACTION');
SELECT pg_temp.reference_by_id('tasks.tasks', 'user_id', 'user_id', 'identity.users', 'NO ACTION');
SELECT pg_temp.reference_by_id('tasks.tasks', 'library_id', 'library_id', 'media.libraries', 'NO ACTION');
SELECT pg_temp.reference_by_id('tasks.tasks', 'channel_id', 'channel_id', 'media.channels', 'NO ACTION');
SELECT pg_temp.reference_by_id('tasks.tasks', 'video_id', 'video_id', 'media.videos', 'NO ACTION');
SELECT pg_temp.reference_by_id('tasks.tasks', 'playlist_id', 'playlist_id', 'media.playlists', 'CASCADE');

-- Nothing references the uuid primary keys any more.
ALTER TABLE access.owners DROP CONSTRAINT owners_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE identity.claims DROP CONSTRAINT claims_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE media.channels DROP CONSTRAINT channels_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE media.creators DROP CONSTRAINT creators_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE media.image_files DROP CONSTRAINT image_files_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE media.libraries DROP CONSTRAINT libraries_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE media.playlists DROP CONSTRAINT playlists_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE media.preferences DROP CONSTRAINT preferences_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE media.sponsorblock_segments DROP CONSTRAINT sponsorblock_segments_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE media.track_files DROP CONSTRAINT track_files_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE media.video_files DROP CONSTRAINT video_files_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE media.videos DROP CONSTRAINT videos_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE tasks.schedules DROP CONSTRAINT schedules_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE tasks.tasks DROP CONSTRAINT tasks_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);
ALTER TABLE tasks.task_runs DROP CONSTRAINT task_runs_pkey, ADD PRIMARY KEY (id), ADD UNIQUE (public_id);

ALTER TABLE identity.users DROP COLUMN public_id, ADD PRIMARY KEY (id);
ALTER TABLE identity.users ADD FOREIGN KEY (id) REFERENCES access.principals (id);

DO
$$
    DECLARE
        reference record;
    BEGIN
        FOR reference IN SELECT * FROM pending_references
            LOOP
                EXECUTE format('ALTER TABLE %s ADD FOREIGN KEY (%I) REFERENCES %s (id) ON DELETE %s',
                               reference.source, reference.column_name, reference.target, reference.on_delete);
            END LOOP;
    END
$$;

DROP FUNCTION pg_temp.reference_by_id(regclass, name, name, regclass, text);

-- Keys, constraints and indexes dropped together with the uuid columns.
ALTER TABLE access.grants ADD PRIMARY KEY (principal_id, owner_id);
CREATE INDEX ON access.grants (owner_id);

ALTER TABLE access.owners ADD UNIQUE (personal_for);

CREATE INDEX ON identity.claims (user_id, claim_type);
CREATE INDEX ON identity.user_logins (user_id, login_provider);

ALTER TABLE media.channel_images ADD PRIMARY KEY (channel_id, image_id);
CREATE INDEX ON media.channel_images (image_id);

ALTER TABLE media.channel_preferences ADD PRIMARY KEY (channel_id, preference_id);

ALTER TABLE media.channel_subscriptions ADD PRIMARY KEY (channel_id);

ALTER TABLE media.creator_channels ADD PRIMARY KEY (creator_id, channel_id);
CREATE UNIQUE INDEX ON media.creator_channels (creator_id) WHERE "primary" = true;

ALTER TABLE media.library_channels ADD PRIMARY KEY (library_id, channel_id);
CREATE UNIQUE INDEX ON media.library_channels (channel_id) WHERE "primary" = true;

ALTER TABLE media.library_external_cookies ADD PRIMARY KEY (library_id);

ALTER TABLE media.library_preferences ADD PRIMARY KEY (library_id, preference_id);

ALTER TABLE media.playlist_images ADD PRIMARY KEY (playlist_id, image_id);

ALTER TABLE media.playlist_videos ADD PRIMARY KEY (playlist_id, "order");
CREATE INDEX ON media.playlist_videos (video_id);

CREATE UNIQUE INDEX ON media.playlists (library_id, external_id);

CREATE INDEX ON media.sponsorblock_segments (video_id);

CREATE INDEX ON media.track_files (video_id);
CREATE UNIQUE INDEX track_files_video_id_chapters_idx ON media.track_files (video_id) WHERE type = 'chapters';
CREATE UNIQUE INDEX track_files_video_id_language_subtitles_idx ON media.track_files (video_id, language) WHERE type = 'subtitles';

CREATE INDEX ON media.video_files (video_id);

ALTER TABLE media.video_files_downloading ADD PRIMARY KEY (file_id);

ALTER TABLE media.video_images ADD PRIMARY KEY (video_id, image_id);
CREATE INDEX ON media.video_images (image_id);

CREATE INDEX ON media.video_plays (video_id, user_id);

ALTER TABLE media.video_viewed_by_users ADD PRIMARY KEY (video_id, user_id);

CREATE INDEX ON media.videos (channel_id);

CREATE INDEX ON tasks.task_requests (task_id);

ALTER TABLE tasks.task_run_progress ADD PRIMARY KEY (run_id);

ALTER TABLE tasks.task_run_results ADD UNIQUE (run_id);

CREATE INDEX ON tasks.task_runs (task_id);

DROP INDEX tasks.tasks_id_type_idx;
CREATE INDEX ON tasks.tasks (id, type);

-- Called in FROM, so that PostgreSQL inlines it into the surrounding query:
-- x.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
-- Arguments must not be subqueries, otherwise the function is not inlined.
CREATE FUNCTION access.accessible_owners(principal bigint, required_permissions integer)
    RETURNS TABLE
            (
                owner_id bigint
            )
    LANGUAGE sql
    STABLE
    PARALLEL SAFE
AS
$$
SELECT grants.owner_id
FROM access.grants
WHERE grants.principal_id = principal
  AND (grants.permissions & required_permissions) = required_permissions
$$;

CREATE VIEW tasks.completed_tasks AS
SELECT tasks.id AS taskid,
       task_runs.id AS taskrunid,
       task_run_results.id AS taskrunresult
FROM tasks.tasks
         JOIN tasks.task_runs ON tasks.id = task_runs.task_id
         JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
ORDER BY task_run_results.created_at;

CREATE VIEW tasks.queued_tasks AS
SELECT tasks.id AS taskid
FROM tasks.tasks
         LEFT JOIN tasks.task_runs ON tasks.id = task_runs.task_id
         LEFT JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
WHERE task_runs.id IS NULL
ORDER BY tasks.created_at;

CREATE VIEW tasks.running_tasks AS
SELECT tasks.id AS taskid,
       tasks.type AS tasktype,
       task_runs.id AS taskrunid,
       task_run_progress.value / task_run_progress.target * 100 AS progress
FROM tasks.tasks
         JOIN tasks.task_runs ON tasks.id = task_runs.task_id
         LEFT JOIN tasks.task_run_progress ON task_runs.id = task_run_progress.run_id
         LEFT JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
WHERE task_run_results.id IS NULL
ORDER BY task_runs.created_at;

ANALYZE;
