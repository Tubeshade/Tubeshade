DELETE
FROM tasks.task_requests
WHERE id = (SELECT id
            FROM tasks.task_requests
            WHERE id = @taskRequestId
                FOR UPDATE SKIP LOCKED)
RETURNING (SELECT tasks.public_id FROM tasks.tasks WHERE tasks.id = task_requests.task_id) AS task_id, source;
