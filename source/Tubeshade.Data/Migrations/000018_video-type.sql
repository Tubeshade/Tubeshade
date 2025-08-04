CREATE TYPE media.video_type AS ENUM ('video', 'short', 'livestream');

ALTER TABLE media.videos
    ADD COLUMN type media.video_type DEFAULT 'video' NOT NULL;

ALTER TABLE media.videos
    ALTER COLUMN type DROP DEFAULT;
