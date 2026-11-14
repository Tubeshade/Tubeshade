WITH playlist AS
    (SELECT playlists.id
     FROM media.playlists
     WHERE playlists.id = @playlistId
       AND playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @LibraryPermissions)))),

     deleted AS
    (DELETE FROM media.playlist_videos
     WHERE playlist_id IN (SELECT id FROM playlist)),

     inserted AS
    (INSERT INTO media.playlist_videos (playlist_id, video_id, "order")
     SELECT playlist.id, entries.video_id, entries.ordinality - 1
     FROM UNNEST(@videoIds) WITH ORDINALITY AS entries(video_id, ordinality)
         CROSS JOIN playlist
         INNER JOIN media.videos ON videos.id = entries.video_id
     WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @VideoPermissions)))

SELECT count(*) FROM playlist;
