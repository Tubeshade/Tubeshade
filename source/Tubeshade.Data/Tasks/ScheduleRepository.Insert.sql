INSERT INTO tasks.schedules (created_by_user_id, modified_by_user_id, owner_id, task_id, cron_expression, time_zone_id)
SELECT @CreatedByUserId, @ModifiedByUserId, @OwnerId, @TaskId, @CronExpression, @TimeZoneId
WHERE @OwnerId IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING id;
