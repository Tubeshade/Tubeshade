WITH matching_videos AS
         (SELECT id, external_url, channel_id
          FROM media.videos
          WHERE public_id = @VideoId
             OR external_url = @Url),

     matching_channels AS
         (SELECT channels.id
          FROM media.channels
                   LEFT OUTER JOIN matching_videos ON channel_id = channels.id
          WHERE matching_videos.id IS NOT NULL
             OR channels.public_id = @ChannelId)

SELECT task_runs.id
FROM tasks.tasks
         INNER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
         LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
WHERE task_run_results.id IS NULL
  AND task_runs.created_at < (SELECT created_at FROM tasks.task_runs WHERE public_id = @RunId)
  AND ((tasks.type = 'youtube_feed_update' AND @Type::tasks.task_type = 'youtube_feed_update')
    OR (tasks.type != 'youtube_feed_update' AND @Type::tasks.task_type != 'youtube_feed_update'))
  AND (tasks.video_id IN (SELECT id FROM matching_videos)
    OR tasks.url IN (SELECT external_url FROM matching_videos)
    OR (tasks.channel_id IN (SELECT id FROM matching_channels))
    OR (@VideoId IS NOT NULL AND tasks.video_id = (SELECT videos.id FROM media.videos WHERE videos.public_id = @VideoId))
    OR (@ChannelId IS NOT NULL AND tasks.channel_id = (SELECT channels.id FROM media.channels WHERE channels.public_id = @ChannelId))
    OR (@PlaylistId IS NOT NULL AND tasks.playlist_id = (SELECT playlists.id FROM media.playlists WHERE playlists.public_id = @PlaylistId))
    OR (@Url IS NOT NULL AND tasks.url = @Url)
    OR (@Type = tasks.type AND tasks.type = 'reindex_videos'));
