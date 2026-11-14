INSERT INTO tasks.task_requests (task_id, source)
SELECT tasks.id, @source::tasks.task_source
FROM tasks.tasks
WHERE tasks.id = @taskId;
