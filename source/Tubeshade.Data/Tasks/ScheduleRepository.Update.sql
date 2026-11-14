UPDATE tasks.schedules
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    task_id = (SELECT tasks.id FROM tasks.tasks WHERE tasks.public_id = @TaskId),
    cron_expression = @CronExpression,
    time_zone_id = @TimeZoneId
WHERE schedules.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))
  AND schedules.public_id = @PublicId;
