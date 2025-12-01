ALTER TYPE tasks.task_type ADD VALUE 'update_sponsor_block_segments';

ALTER TABLE media.sponsorblock_segments
    ADD COLUMN modified_at         timestamptz           NULL,
    ADD COLUMN modified_by_user_id uuid                  NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    ADD COLUMN locked              boolean DEFAULT FALSE NOT NULL;

UPDATE media.sponsorblock_segments
SET modified_at         = created_at,
    modified_by_user_id = created_by_user_id
WHERE sponsorblock_segments.modified_at IS NULL;

ALTER TABLE media.sponsorblock_segments
    ALTER COLUMN modified_at SET NOT NULL,
    ALTER COLUMN modified_at SET DEFAULT CURRENT_TIMESTAMP,
    ALTER COLUMN modified_by_user_id SET NOT NULL,
    ALTER COLUMN locked DROP DEFAULT;

WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM'),
     reindex_tasks AS (SELECT id FROM tasks.tasks WHERE tasks.type = 'reindex_videos'),
     reindex_schedules AS (SELECT schedules.id
                           FROM tasks.schedules
                                    INNER JOIN reindex_tasks ON task_id = reindex_tasks.id)

UPDATE tasks.schedules
SET cron_expression     = '*/15 * * * *',
    modified_at         = CURRENT_TIMESTAMP,
    modified_by_user_id = system.id
FROM reindex_schedules
         CROSS JOIN system
WHERE schedules.id IN (SELECT id FROM reindex_schedules);
