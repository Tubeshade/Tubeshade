ALTER TYPE tasks.task_type ADD VALUE 'refresh_file_metadata';

CREATE TYPE media.hash_algorithm AS ENUM ('placeholder', 'SHA256');

ALTER TABLE media.image_files
    ADD COLUMN hash           bytea                DEFAULT ''::bytea     NOT NULL,
    ADD COLUMN hash_algorithm media.hash_algorithm DEFAULT 'placeholder' NOT NULL,
    ADD COLUMN storage_size   bigint               DEFAULT 0             NOT NULL;

ALTER TABLE media.image_files
    ALTER COLUMN hash DROP DEFAULT,
    ALTER COLUMN hash_algorithm DROP DEFAULT,
    ALTER COLUMN storage_size DROP DEFAULT;

ALTER TABLE media.video_files
    ADD COLUMN hash           bytea                NULL,
    ADD COLUMN hash_algorithm media.hash_algorithm NULL,
    ADD COLUMN storage_size   bigint               NULL;
