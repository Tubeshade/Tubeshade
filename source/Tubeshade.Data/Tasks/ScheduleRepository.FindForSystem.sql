SELECT id,
       public_id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       owner_id,
       (SELECT tasks.public_id FROM tasks.tasks WHERE tasks.id = schedules.task_id) AS task_id,
       cron_expression,
       time_zone_id
FROM tasks.schedules
WHERE schedules.public_id = @Id;
