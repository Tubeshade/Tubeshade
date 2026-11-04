CREATE TABLE media.video_plays
(
    id        uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY NOT DEFERRABLE,
    video_id  uuid                                   NOT NULL REFERENCES media.videos (id) ON DELETE CASCADE NOT DEFERRABLE,
    user_id   uuid                                   NOT NULL REFERENCES identity.users (id) ON DELETE CASCADE NOT DEFERRABLE,
    played_at timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL
);

CREATE INDEX ON media.video_plays (video_id, user_id);

ALTER TABLE media.video_viewed_by_users
    ADD COLUMN watch_time    numeric NOT NULL DEFAULT 0,
    ADD COLUMN playback_time numeric NOT NULL DEFAULT 0;

-- only the latest time a video was viewed is known
INSERT INTO media.video_plays (video_id, user_id, played_at)
SELECT video_viewed_by_users.video_id,
       video_viewed_by_users.user_id,
       video_viewed_by_users.modified_at
FROM media.video_viewed_by_users
WHERE video_viewed_by_users.viewed;
