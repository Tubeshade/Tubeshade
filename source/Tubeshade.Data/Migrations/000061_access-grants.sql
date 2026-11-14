CREATE SCHEMA access;

ALTER TABLE identity.owners SET SCHEMA access;

-- A user's own owner. Existing users' owners share the user's id.
ALTER TABLE access.owners
    ADD COLUMN personal_for uuid NULL UNIQUE REFERENCES identity.users (id) ON DELETE CASCADE;

UPDATE access.owners
SET personal_for = owners.id
WHERE EXISTS(SELECT 1 FROM identity.users WHERE users.id = owners.id);

CREATE TABLE access.grants
(
    principal_id uuid        NOT NULL REFERENCES identity.users (id) ON DELETE CASCADE,
    owner_id     uuid        NOT NULL REFERENCES access.owners (id) ON DELETE CASCADE,
    permissions  integer     NOT NULL CHECK (permissions > 0),
    granted_by   uuid        NOT NULL REFERENCES identity.users (id),
    granted_at   timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (principal_id, owner_id)
);

CREATE INDEX ON access.grants (owner_id);

INSERT INTO access.grants (principal_id, owner_id, permissions, granted_by, granted_at)
SELECT ownerships.user_id,
       ownerships.owner_id,
       bit_or(CASE ownerships.access
                  WHEN 'read' THEN 1
                  WHEN 'append' THEN 2
                  WHEN 'modify' THEN 4
                  WHEN 'delete' THEN 8
                  WHEN 'owner' THEN 31
           END),
       (array_agg(ownerships.created_by_user_id ORDER BY ownerships.created_at, ownerships.id))[1],
       min(ownerships.created_at)
FROM identity.ownerships
GROUP BY ownerships.user_id, ownerships.owner_id;

-- The system user no longer reaches users' data through grants (added by 000043_system-access.sql);
-- background work that belongs to no user goes through explicit system methods instead.
-- Users keep Read on the system owner.
DELETE
FROM access.grants
    USING identity.users
WHERE users.normalized_name = 'SYSTEM'
  AND grants.principal_id = users.id
  AND grants.owner_id != users.id;

-- Tasks for a library belong to the library's owner. Those that ran as the system user
-- (reindexing, webhook feed updates, startup migrations) now run as the user whose personal owner owns the library.
UPDATE tasks.tasks
SET owner_id = libraries.owner_id,
    user_id  = CASE
                   WHEN tasks.user_id IS NULL OR tasks.user_id = system.id
                       THEN COALESCE(owners.personal_for, tasks.user_id)
                   ELSE tasks.user_id
        END
FROM media.libraries
         INNER JOIN access.owners ON owners.id = libraries.owner_id,
     identity.users system
WHERE tasks.library_id = libraries.id
  AND system.normalized_name = 'SYSTEM';

UPDATE tasks.schedules
SET owner_id = tasks.owner_id
FROM tasks.tasks
WHERE schedules.task_id = tasks.id
  AND tasks.library_id IS NOT NULL;

-- Files belong to their video's owner, as new files do. Files of uploaded videos were owned by the uploader,
-- or by the system user, which would make them readable by every user and leave them behind when the video is deleted.
UPDATE media.video_files
SET owner_id = videos.owner_id
FROM media.videos
WHERE video_files.video_id = videos.id
  AND video_files.owner_id != videos.owner_id;

-- Called in FROM, so that PostgreSQL inlines it into the surrounding query:
-- x.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
CREATE FUNCTION access.accessible_owners(principal uuid, required_permissions integer)
    RETURNS TABLE
            (
                owner_id uuid
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

DROP TABLE identity.ownerships;
DROP TYPE identity.access;
