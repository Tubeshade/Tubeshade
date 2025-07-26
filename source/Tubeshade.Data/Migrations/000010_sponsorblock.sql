CREATE TYPE media.sponsorblock_segment_action AS ENUM ('skip', 'mute', 'full', 'poi', 'chapter', 'filler');
CREATE TYPE media.sponsorblock_segment_category AS ENUM ('sponsor', 'selfpromo', 'interaction', 'intro', 'outro', 'preview', 'music_offtopic', 'filter');

CREATE TABLE media.sponsorblock_segments
(
    id                 uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    video_id           uuid                                   NOT NULL REFERENCES media.videos (id) NOT DEFERRABLE,
    external_id        text                                   NOT NULL,

    start_time         decimal                                NOT NULL,
    end_time           decimal                                NOT NULL,
    category           media.sponsorblock_segment_category    NOT NULL,
    action             media.sponsorblock_segment_action      NOT NULL,
    description        text                                   NULL
);
