-- Preferences are only reachable once they are linked to a library or channel, so there is no access check here
INSERT INTO media.preferences (created_by_user_id, modified_by_user_id, playback_speed, videos_count, live_streams_count, shorts_count, player_client, download_videos, download_method)
VALUES (@CreatedByUserId, @ModifiedByUserId, @PlaybackSpeed, @VideosCount, @LiveStreamsCount, @ShortsCount, @PlayerClient, @DownloadVideos, @DownloadMethod)
RETURNING public_id;
