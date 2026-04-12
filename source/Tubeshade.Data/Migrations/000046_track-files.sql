CREATE TABLE media.track_files
(
    id                  uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id  uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    video_id            uuid                                   NOT NULL REFERENCES media.videos (id) NOT DEFERRABLE,
    storage_path        text                                   NOT NULL,
    type                media.track_type                       NOT NULL,
    language            text                                   NULL,
    hash                bytea                                  NOT NULL,
    hash_algorithm      media.hash_algorithm                   NOT NULL,
    storage_size        bigint                                 NOT NULL,

    CONSTRAINT track_files_subtitles_language_check CHECK (type != 'subtitles' OR language IS NOT NULL) NOT DEFERRABLE
);

CREATE UNIQUE INDEX track_files_video_id_chapters_idx ON media.track_files (video_id) WHERE (type = 'chapters');
CREATE UNIQUE INDEX track_files_video_id_language_subtitles_idx ON media.track_files (video_id, language) WHERE (type = 'subtitles');
