SELECT id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       owner_id,
       task_id,
       cron_expression,
       time_zone_id
FROM tasks.schedules
WHERE schedules.id = @Id;
