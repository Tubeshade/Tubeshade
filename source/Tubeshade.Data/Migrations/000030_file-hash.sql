CREATE TYPE media.hash_algorithm AS ENUM ('placeholder', 'SHA256');

ALTER TABLE media.image_files
    ADD COLUMN hash           bytea                DEFAULT ''::bytea     NOT NULL,
    ADD COLUMN hash_algorithm media.hash_algorithm DEFAULT 'placeholder' NOT NULL;

ALTER TABLE media.image_files
    ALTER COLUMN hash DROP DEFAULT,
    ALTER COLUMN hash_algorithm DROP DEFAULT;

ALTER TABLE media.video_files
    ADD COLUMN hash           bytea                NULL,
    ADD COLUMN hash_algorithm media.hash_algorithm NULL;
