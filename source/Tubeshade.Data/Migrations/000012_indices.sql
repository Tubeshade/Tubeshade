CREATE INDEX ON media.video_files (video_id);
CREATE INDEX ON media.sponsorblock_segments (video_id);
CREATE INDEX ON media.channels (external_id);
CREATE INDEX ON media.library_external_cookies (id, domain);

CREATE INDEX ON tasks.tasks (id, type);
CREATE INDEX ON tasks.task_runs (task_id);

ANALYSE;
