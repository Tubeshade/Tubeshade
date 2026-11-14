-- FOR NO KEY UPDATE instead of FOR UPDATE, so that it does not conflict with FOR KEY SHARE locks
-- taken by foreign key checks, for example when a task is triggered concurrently.
SELECT id
FROM tasks.tasks
WHERE public_id = @taskId
    FOR NO KEY UPDATE SKIP LOCKED;
