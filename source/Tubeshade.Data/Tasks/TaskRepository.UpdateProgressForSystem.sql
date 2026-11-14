UPDATE tasks.task_run_progress
SET modified_at = CURRENT_TIMESTAMP,
    value = @newValue,
    rate = @rate,
    remaining_duration = @remainingDuration
FROM tasks.task_runs
WHERE task_runs.id = task_run_progress.run_id
  AND task_runs.public_id = @taskRunId;
