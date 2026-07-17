DROP INDEX IF EXISTS videos_channel_id_idx;
CREATE INDEX videos_channel_id_idx ON media.videos (channel_id);
