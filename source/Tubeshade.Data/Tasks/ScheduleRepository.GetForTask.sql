SELECT schedules.id,
       schedules.public_id,
       schedules.created_at,
       schedules.created_by_user_id,
       schedules.modified_at,
       schedules.modified_by_user_id,
       schedules.owner_id,
       tasks.public_id AS task_id,
       schedules.cron_expression,
       schedules.time_zone_id
FROM tasks.schedules
    INNER JOIN tasks.tasks ON schedules.task_id = tasks.id
WHERE schedules.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)) AND
      tasks.type = @type AND
      tasks.library_id = (SELECT libraries.id FROM media.libraries WHERE libraries.public_id = @libraryId)
