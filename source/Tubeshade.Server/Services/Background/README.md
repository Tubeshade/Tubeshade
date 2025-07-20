1. `TaskListenerBackgroundService`:
   1. listens for `task_created` notification from the database
   2. writes to the corresponding `Channel` depending on `TaskType`
2. Each `TaskType` has a separate `BackgroundService`, which reads from the corresponding `Channel`
