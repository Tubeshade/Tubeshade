WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM'),
     reindex_tasks AS (
         INSERT INTO tasks.tasks (created_by_user_id, modified_by_user_id, owner_id, type)
         SELECT system.id, system.id, system.id, 'reindex_videos'
         FROM system
         RETURNING id)

INSERT
INTO tasks.schedules (created_by_user_id, modified_by_user_id, task_id, cron_expression, time_zone_id, owner_id)
SELECT system.id, system.id, reindex_tasks.id, '* * * * *', 'Etc/UTC', system.id
FROM reindex_tasks;
