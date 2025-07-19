ALTER TYPE tasks.task_type ADD VALUE 'scan_channel';

DROP TRIGGER tasks_created ON tasks.tasks;
