WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM'),
     libraries AS (SELECT libraries.id, libraries.owner_id, system.id AS user_id FROM media.libraries CROSS JOIN system),
     reindex_tasks AS (
         INSERT INTO tasks.tasks (created_by_user_id, modified_by_user_id, owner_id, type, user_id, library_id)
         SELECT libraries.user_id, libraries.user_id, libraries.owner_id, 'reindex_videos', libraries.user_id, libraries.id
         FROM libraries
         RETURNING id, created_by_user_id AS user_id)

INSERT
INTO tasks.schedules (created_by_user_id, modified_by_user_id, task_id, cron_expression, time_zone_id, owner_id)
SELECT reindex_tasks.user_id, reindex_tasks.user_id, reindex_tasks.id, '* * * * *', 'Etc/UTC', reindex_tasks.user_id
FROM reindex_tasks;
