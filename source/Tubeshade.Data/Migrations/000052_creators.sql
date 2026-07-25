CREATE TABLE media.creators
(
    id                  uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY NOT DEFERRABLE,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id  uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    owner_id            uuid                                   NOT NULL REFERENCES identity.owners (id) NOT DEFERRABLE,
    name                text                                   NOT NULL
);

CREATE TABLE media.creator_channels
(
    creator_id uuid NOT NULL REFERENCES media.creators (id) ON DELETE CASCADE NOT DEFERRABLE,
    channel_id uuid NOT NULL REFERENCES media.channels (id) ON DELETE CASCADE NOT DEFERRABLE,
    "primary"  bool NOT NULL,

    PRIMARY KEY (creator_id, channel_id)
);

CREATE UNIQUE INDEX ON media.creator_channels (creator_id)
    WHERE creator_channels."primary" = true;
