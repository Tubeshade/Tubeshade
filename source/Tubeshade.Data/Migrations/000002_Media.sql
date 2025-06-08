DROP SCHEMA IF EXISTS media CASCADE;
CREATE SCHEMA media;

CREATE TABLE media.libraries
(
    id                  uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id  uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    owner_id            uuid                                   NOT NULL REFERENCES identity.owners (id) NOT DEFERRABLE,
    name                text                                   NOT NULL,

    storage_path        text                                   NOT NULL
);

CREATE TABLE media.library_external_cookies
(
    id                  uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY REFERENCES media.libraries (id) NOT DEFERRABLE,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id  uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    domain              text                                   NOT NULL,
    cookie              text                                   NOT NULL
);

CREATE TABLE media.channels
(
    id                  uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id  uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    owner_id            uuid                                   NOT NULL REFERENCES identity.owners (id) NOT DEFERRABLE,
    name                text                                   NOT NULL,

    storage_path        text                                   NOT NULL,
    external_id         text                                   NOT NULL,
    subscribed_at       timestamptz                            NULL
);

CREATE TABLE media.library_channels
(
    library_id uuid NOT NULL REFERENCES media.libraries (id) NOT DEFERRABLE,
    channel_id uuid NOT NULL REFERENCES media.channels (id) NOT DEFERRABLE,

    PRIMARY KEY (library_id, channel_id)
);

CREATE TYPE media.external_availability AS ENUM ('public', 'private', 'not_available');

CREATE TABLE media.videos
(
    id                    uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at            timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id    uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at           timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id   uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    owner_id              uuid                                   NOT NULL REFERENCES identity.owners (id) NOT DEFERRABLE,
    channel_id            uuid                                   NOT NULL REFERENCES media.channels (id) NOT DEFERRABLE,
    storage_path          text                                   NOT NULL,

    external_id           text                                   NOT NULL,
    external_url          text                                   NOT NULL,
    name                  text                                   NOT NULL,
    description           text                                   NOT NULL,
    categories            text[]                                 NOT NULL,
    tags                  text[]                                 NOT NULL,
    published_at          timestamptz                            NOT NULL,
    refreshed_at          timestamptz                            NOT NULL,
    availability          media.external_availability            NOT NULL,
    duration              interval                               NOT NULL,
    view_count            bigint                                 NULL,
    like_count            bigint                                 NULL,

    downloaded_at         timestamptz                            NULL,
    downloaded_by_user_id uuid                                   NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    ignored_at            timestamptz                            NULL,
    ignored_by_user_id    uuid                                   NULL REFERENCES identity.users (id) NOT DEFERRABLE
);

CREATE TABLE media.preferences
(
    id                  uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id  uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    playback_speed      decimal                                NULL
);

CREATE TABLE media.library_preferences
(
    library_id    uuid NOT NULL REFERENCES media.libraries (id) NOT DEFERRABLE,
    preference_id uuid NOT NULL REFERENCES media.preferences (id) NOT DEFERRABLE,

    PRIMARY KEY (library_id, preference_id)
);

CREATE TABLE media.channel_preferences
(
    channel_id    uuid NOT NULL REFERENCES media.channels (id) NOT DEFERRABLE,
    preference_id uuid NOT NULL REFERENCES media.preferences (id) NOT DEFERRABLE,

    PRIMARY KEY (channel_id, preference_id)
);
