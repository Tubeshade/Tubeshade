CREATE TABLE media.video_files_downloading
(
    file_id     uuid NOT NULL REFERENCES media.video_files (id) ON DELETE CASCADE NOT DEFERRABLE PRIMARY KEY,
    task_run_id uuid NOT NULL REFERENCES tasks.task_runs (id) ON DELETE CASCADE NOT DEFERRABLE,
    path        text NOT NULL
);
