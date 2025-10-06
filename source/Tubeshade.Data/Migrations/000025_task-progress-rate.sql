ALTER TABLE tasks.task_run_progress
    ADD COLUMN rate               decimal  NULL,
    ADD COLUMN remaining_duration interval NULL;
