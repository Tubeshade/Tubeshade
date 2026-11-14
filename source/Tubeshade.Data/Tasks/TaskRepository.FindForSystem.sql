SELECT id,
       public_id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       owner_id,
       type,
       user_id,
       (SELECT libraries.public_id FROM media.libraries WHERE libraries.id = tasks.library_id) AS library_id,
       (SELECT channels.public_id FROM media.channels WHERE channels.id = tasks.channel_id) AS channel_id,
       (SELECT playlists.public_id FROM media.playlists WHERE playlists.id = tasks.playlist_id) AS playlist_id,
       (SELECT videos.public_id FROM media.videos WHERE videos.id = tasks.video_id) AS video_id,
       url,
       all_videos,
       payload
FROM tasks.tasks
WHERE tasks.public_id = @Id;
