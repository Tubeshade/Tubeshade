UPDATE tasks.task_run_progress
SET modified_at = CURRENT_TIMESTAMP,
    value = @newValue,
    rate = @rate,
    remaining_duration = @remainingDuration
WHERE run_id = @taskRunId;
