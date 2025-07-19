ALTER TABLE media.channels
    ADD COLUMN subscriber_count int                                          NULL,
    ADD COLUMN external_url     text                                         NULL,
    ADD COLUMN availability     media.external_availability DEFAULT 'public' NOT NULL;

UPDATE media.channels
SET external_url = 'https://youtube.com/channel/' || external_id
WHERE external_id != '';

ALTER TABLE media.channels
    ALTER COLUMN external_url SET NOT NULL,
    ALTER COLUMN availability DROP DEFAULT;
