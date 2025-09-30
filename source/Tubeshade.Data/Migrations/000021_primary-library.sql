ALTER TABLE media.library_channels
    ADD COLUMN "primary" bool NOT NULL DEFAULT TRUE;

CREATE UNIQUE INDEX ON media.library_channels (channel_id)
    WHERE library_channels."primary" = true;

ALTER TABLE media.library_channels
    ALTER COLUMN "primary" DROP DEFAULT;
