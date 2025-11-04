ALTER TABLE tasks.tasks
    ADD COLUMN user_id    uuid    NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    ADD COLUMN library_id uuid    NULL REFERENCES media.libraries (id) NOT DEFERRABLE,
    ADD COLUMN channel_id uuid    NULL REFERENCES media.channels (id) NOT DEFERRABLE,
    ADD COLUMN video_id   uuid    NULL REFERENCES media.videos (id) NOT DEFERRABLE,
    ADD COLUMN url        text    NULL,
    ADD COLUMN all_videos boolean NOT NULL DEFAULT false;

UPDATE tasks.tasks
SET user_id    = (payload::json ->> 'userId')::uuid,
    library_id = (payload::json ->> 'libraryId')::uuid,
    channel_id = (payload::json ->> 'channelId')::uuid,
    video_id   = (payload::json ->> 'videoId')::uuid,
    url        = (payload::json ->> 'url')::text,
    all_videos = COALESCE((payload::json ->> 'all')::boolean, false)
WHERE payload != '';

ALTER TABLE tasks.tasks
    DROP COLUMN payload;
