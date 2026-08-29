CREATE TABLE media.playlists
(
    id                  uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY NOT DEFERRABLE,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id  uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    -- access to a playlist is controlled through the library that contains it
    library_id          uuid                                   NOT NULL REFERENCES media.libraries (id) NOT DEFERRABLE,

    storage_path        text                                   NOT NULL,
    external_id         text                                   NOT NULL,
    external_url        text                                   NOT NULL,
    name                text                                   NOT NULL,
    refreshed_at        timestamptz                            NOT NULL,
    subscribed_at       timestamptz                            NULL
);

CREATE UNIQUE INDEX ON media.playlists (library_id, external_id);

CREATE TABLE media.playlist_videos
(
    playlist_id uuid    NOT NULL REFERENCES media.playlists (id) ON DELETE CASCADE NOT DEFERRABLE,
    video_id    uuid    NOT NULL REFERENCES media.videos (id) ON DELETE CASCADE NOT DEFERRABLE,
    "order"     integer NOT NULL,

    PRIMARY KEY (playlist_id, video_id) NOT DEFERRABLE,

    -- deferred, so that a rescan can shift the positions of existing entries within a single statement
    UNIQUE (playlist_id, "order") DEFERRABLE INITIALLY DEFERRED
);

CREATE INDEX ON media.playlist_videos (video_id);

CREATE TABLE media.playlist_images
(
    playlist_id uuid NOT NULL REFERENCES media.playlists (id) ON DELETE CASCADE NOT DEFERRABLE,
    image_id    uuid NOT NULL REFERENCES media.image_files (id) ON DELETE CASCADE NOT DEFERRABLE,

    PRIMARY KEY (playlist_id, image_id) NOT DEFERRABLE
);

ALTER TABLE tasks.tasks
    ADD COLUMN playlist_id uuid NULL REFERENCES media.playlists (id) ON DELETE CASCADE NOT DEFERRABLE;

ALTER TYPE tasks.task_type ADD VALUE 'scan_playlist';
