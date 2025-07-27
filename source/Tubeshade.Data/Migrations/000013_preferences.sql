ALTER TABLE media.preferences
    ADD COLUMN videos_count       integer NULL,
    ADD COLUMN live_streams_count integer NULL,
    ADD COLUMN shorts_count       integer NULL;
