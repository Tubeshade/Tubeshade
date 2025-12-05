CREATE TYPE media.download_videos AS ENUM ('all', 'new', 'none');

ALTER TABLE media.preferences
    ADD COLUMN download_videos media.download_videos NULL;

WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
UPDATE media.preferences
SET modified_at         = CURRENT_TIMESTAMP,
    modified_by_user_id = system.id,
    download_videos     = 'all'
FROM system
WHERE preferences.download_automatically = TRUE;

WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
UPDATE media.preferences
SET modified_at         = CURRENT_TIMESTAMP,
    modified_by_user_id = system.id,
    download_videos     = 'none'
FROM system
WHERE preferences.download_automatically = FALSE;

ALTER TABLE media.preferences
    DROP COLUMN download_automatically;
