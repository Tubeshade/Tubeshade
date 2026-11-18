WITH playlist AS
    (SELECT playlists.id
     FROM media.playlists
     WHERE playlists.public_id = @playlistId
       AND playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @LibraryPermissions)))),

     -- sub-statements cannot see each other's changes, so existing positions are overwritten instead of deleted first
     upserted AS
    (INSERT INTO media.playlist_videos (playlist_id, video_id, "order")
     SELECT playlist.id, videos.id, entries.ordinality - 1
     FROM UNNEST(@videoIds) WITH ORDINALITY AS entries(video_id, ordinality)
         CROSS JOIN playlist
         INNER JOIN media.videos ON videos.public_id = entries.video_id
     WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @VideoPermissions))
     ON CONFLICT (playlist_id, "order") DO UPDATE SET video_id = excluded.video_id
     RETURNING "order"),

     deleted AS
    (DELETE FROM media.playlist_videos
     WHERE playlist_id IN (SELECT id FROM playlist)
       AND "order" NOT IN (SELECT "order" FROM upserted))

SELECT count(*) FROM playlist;
