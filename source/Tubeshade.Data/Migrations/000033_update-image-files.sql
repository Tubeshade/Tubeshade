ALTER TABLE media.image_files
    ADD COLUMN modified_at         timestamptz NULL,
    ADD COLUMN modified_by_user_id uuid        NULL REFERENCES identity.users (id) NOT DEFERRABLE;

UPDATE media.image_files
SET modified_at         = created_at,
    modified_by_user_id = created_by_user_id
WHERE image_files.modified_at IS NULL;

ALTER TABLE media.image_files
    ALTER COLUMN modified_at SET NOT NULL,
    ALTER COLUMN modified_at SET DEFAULT CURRENT_TIMESTAMP,
    ALTER COLUMN modified_by_user_id SET NOT NULL;
