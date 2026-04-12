CREATE TYPE media.track_type AS ENUM ('subtitles', 'chapters');
ALTER TYPE tasks.task_type ADD VALUE 'refresh_track_files';
