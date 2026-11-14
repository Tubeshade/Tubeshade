SELECT libraries.id,
       libraries.public_id,
       libraries.created_at,
       libraries.created_by_user_id,
       libraries.modified_at,
       libraries.modified_by_user_id,
       libraries.owner_id,
       libraries.name,
       libraries.storage_path,
       schedules.public_id AS subscriptions_schedule_id
FROM media.libraries
    INNER JOIN tasks.schedules ON schedules.id = libraries.subscriptions_schedule_id
WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
