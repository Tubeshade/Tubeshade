INSERT INTO tasks.schedules (created_by_user_id, modified_by_user_id, owner_id, task_id, cron_expression, time_zone_id)
SELECT @CreatedByUserId, @ModifiedByUserId, @OwnerId, tasks.id, @CronExpression, @TimeZoneId
FROM tasks.tasks
WHERE tasks.public_id = @TaskId
  AND @OwnerId IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING public_id;
