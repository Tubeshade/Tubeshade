DELETE
FROM tasks.task_requests
WHERE id = (SELECT id
            FROM tasks.task_requests
            WHERE id = @taskRequestId
                FOR UPDATE)
RETURNING task_id, source;
