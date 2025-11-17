ALTER TABLE media.video_viewed_by_users
    ADD COLUMN modified_at timestamptz NULL,
    ADD COLUMN viewed      boolean     NOT NULL DEFAULT true,
    ADD COLUMN position    numeric     NULL;

UPDATE media.video_viewed_by_users
SET modified_at = created_at;

ALTER TABLE media.video_viewed_by_users
    ALTER COLUMN modified_at SET DEFAULT CURRENT_TIMESTAMP,
    ALTER COLUMN modified_at SET NOT NULL,
    ALTER COLUMN viewed DROP DEFAULT;
