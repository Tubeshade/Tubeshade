WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
INSERT
INTO tasks.tasks (created_by_user_id, modified_by_user_id, owner_id, type, payload)
SELECT system.id, system.id, system.id, 'refresh_subscriptions', ''
FROM system;

WITH task AS (SELECT id, created_by_user_id FROM tasks.tasks WHERE type = 'refresh_subscriptions')
INSERT
INTO tasks.schedules (created_by_user_id, modified_by_user_id, task_id, cron_expression, time_zone_id, owner_id)
SELECT task.created_by_user_id, task.created_by_user_id, task.id, '0 */4 * * *', 'Etc/UTC', task.created_by_user_id
FROM task;
