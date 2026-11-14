WITH video AS (SELECT videos.id FROM media.videos WHERE videos.public_id = @videoId)

SELECT plays.play_count AS PlayCount,
       coalesce(video_viewed_by_users.watch_time, 0)::double precision AS WatchTime,
       coalesce(video_viewed_by_users.playback_time, 0)::double precision AS PlaybackTime
FROM (SELECT count(video_plays.id)::integer AS play_count
      FROM media.video_plays
      WHERE video_plays.video_id = (SELECT video.id FROM video)
        AND video_plays.user_id = @userId) AS plays
     LEFT JOIN media.video_viewed_by_users
               ON video_viewed_by_users.video_id = (SELECT video.id FROM video)
                   AND video_viewed_by_users.user_id = @userId;
