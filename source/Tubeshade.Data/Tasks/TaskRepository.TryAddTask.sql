WITH matching_videos AS
    (SELECT id, external_url, channel_id
     FROM media.videos
     WHERE public_id = @VideoId
        OR external_url = @Url),

    new_task AS
    (SELECT @CreatedByUserId AS created_by_user_id,
            @ModifiedByUserId AS modified_by_user_id,
            COALESCE((SELECT libraries.owner_id FROM media.libraries WHERE libraries.public_id = @LibraryId), @OwnerId) AS owner_id,
            @Type::tasks.task_type AS type,
            @UserId AS user_id,
            (SELECT libraries.id FROM media.libraries WHERE libraries.public_id = @LibraryId) AS library_id,
            (SELECT channels.id FROM media.channels WHERE channels.public_id = @ChannelId) AS channel_id,
            (SELECT playlists.id FROM media.playlists WHERE playlists.public_id = @PlaylistId) AS playlist_id,
            (SELECT videos.id FROM media.videos WHERE videos.public_id = @VideoId) AS video_id,
            @Url AS url,
            @AllVideos AS all_videos,
            @Payload AS payload
     WHERE NOT EXISTS (SELECT task_runs.id
                       FROM tasks.tasks
                           INNER JOIN tasks.task_runs ON tasks.id = tasks.task_runs.task_id
                           LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
                       WHERE task_run_results.id IS NULL
                         AND tasks.library_id = (SELECT libraries.id FROM media.libraries WHERE libraries.public_id = @LibraryId)
                         AND tasks.type = @Type
                         AND (tasks.video_id IN (SELECT id FROM matching_videos)
                           OR tasks.url IN (SELECT external_url FROM matching_videos)
                           OR (@Url IS NOT NULL AND tasks.url = @Url)
                           OR (@PlaylistId IS NOT NULL AND tasks.playlist_id = (SELECT playlists.id FROM media.playlists WHERE playlists.public_id = @PlaylistId)))))

INSERT INTO tasks.tasks (created_by_user_id, modified_by_user_id, owner_id, type, user_id, library_id, channel_id, playlist_id, video_id, url, all_videos, payload)
SELECT created_by_user_id, modified_by_user_id, owner_id, type, user_id, library_id, channel_id, playlist_id, video_id, url, all_videos, payload
FROM new_task
WHERE owner_id IS NOT NULL
  AND owner_id IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING public_id;
