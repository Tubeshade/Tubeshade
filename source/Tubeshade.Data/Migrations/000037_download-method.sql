CREATE TYPE media.download_method AS ENUM ('standard', 'streaming');

ALTER TABLE media.preferences
    ADD COLUMN download_method media.download_method NULL;
