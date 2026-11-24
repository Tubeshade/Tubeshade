ALTER TABLE media.preferences
    ALTER player_client TYPE text;

WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
UPDATE media.preferences
SET modified_at         = CURRENT_TIMESTAMP,
    modified_by_user_id = system.id,
    player_client       = NULL
FROM system
WHERE preferences.player_client = 'tv_embedded';

DROP TYPE media.player_client;
CREATE TYPE media.player_client AS ENUM ('web', 'web_safari', 'mweb', 'tv', 'tv_simply', 'web_embedded', 'web_music', 'web_creator', 'android', 'android_vr', 'ios', 'ios_producer', 'visionos', 'tv_downgraded', 'tv_samsung');

ALTER TABLE media.preferences
    ALTER player_client TYPE media.player_client USING player_client::media.player_client;
