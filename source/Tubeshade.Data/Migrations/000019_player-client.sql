CREATE TYPE media.player_client AS ENUM ('web', 'web_safari', 'mweb', 'tv', 'tv_simply', 'tv_embedded', 'web_embedded', 'web_music', 'web_creator', 'android', 'android_vr', 'ios');

ALTER TABLE media.preferences
    ADD COLUMN player_client media.player_client NULL;
