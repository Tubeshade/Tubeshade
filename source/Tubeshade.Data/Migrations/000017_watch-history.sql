CREATE TABLE media.video_viewed_by_users
(
    video_id   uuid                                  NOT NULL REFERENCES media.videos (id) ON DELETE CASCADE NOT DEFERRABLE,
    user_id    uuid                                  NOT NULL references identity.users (id) ON DELETE CASCADE NOT DEFERRABLE,
    created_at timestamptz DEFAULT CURRENT_TIMESTAMP NOT NULL,

    PRIMARY KEY (video_id, user_id) NOT DEFERRABLE
);
