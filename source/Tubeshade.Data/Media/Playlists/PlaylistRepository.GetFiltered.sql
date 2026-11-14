WITH parameters AS MATERIALIZED (SELECT websearch_to_tsquery('english', @Query) AS query),

    filtered AS (
        SELECT id,
               public_id,
               created_at,
               created_by_user_id,
               modified_at,
               modified_by_user_id,
               (SELECT libraries.public_id FROM media.libraries WHERE libraries.id = playlists.library_id) AS library_id,
               storage_path,
               external_id,
               external_url,
               name,
               refreshed_at,
               subscribed_at,
               (SELECT count(*)
                FROM media.playlist_videos
                WHERE playlist_videos.playlist_id = playlists.id) AS video_count,
               count(*) OVER()                                    AS total_count
        FROM media.playlists
            CROSS JOIN parameters
        WHERE (playlists.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))))
          AND (@LibraryId IS NULL OR playlists.library_id = (SELECT libraries.id FROM media.libraries WHERE libraries.public_id = @LibraryId))
          AND (@Query IS NULL OR playlists.name @@ query)
        ORDER BY
                 CASE WHEN @SortKey = 'random' THEN random() END,
                 CASE WHEN @SortKey = 'created_at' AND NOT @SortDescending THEN playlists.created_at END ASC NULLS LAST,
                 CASE WHEN @SortKey = 'created_at' AND @SortDescending THEN playlists.created_at END DESC NULLS LAST,
                 CASE WHEN @SortKey = 'modified_at' AND NOT @SortDescending THEN playlists.modified_at END ASC NULLS LAST,
                 CASE WHEN @SortKey = 'modified_at' AND @SortDescending THEN playlists.modified_at END DESC NULLS LAST,
                 CASE WHEN @SortKey = 'subscribed_at' AND NOT @SortDescending THEN playlists.subscribed_at END ASC NULLS LAST,
                 CASE WHEN @SortKey = 'subscribed_at' AND @SortDescending THEN playlists.subscribed_at END DESC NULLS LAST,
                 CASE WHEN @SortKey = 'name' AND NOT @SortDescending THEN playlists.name END ASC NULLS LAST,
                 CASE WHEN @SortKey = 'name' AND @SortDescending THEN playlists.name END DESC NULLS LAST,
                 CASE WHEN @SortKey = 'refreshed_at' AND NOT @SortDescending THEN playlists.refreshed_at END ASC NULLS LAST,
                 CASE WHEN @SortKey = 'refreshed_at' AND @SortDescending THEN playlists.refreshed_at END DESC NULLS LAST,
                 CASE WHEN @SortKey = 'video_count' AND NOT @SortDescending THEN (SELECT count(*) FROM media.playlist_videos WHERE playlist_videos.playlist_id = playlists.id) END ASC NULLS LAST,
                 CASE WHEN @SortKey = 'video_count' AND @SortDescending THEN (SELECT count(*) FROM media.playlist_videos WHERE playlist_videos.playlist_id = playlists.id) END DESC NULLS LAST,
                 playlists.id
        LIMIT @Limit
        OFFSET @Offset)

SELECT playlists.id,
       playlists.public_id,
       playlists.created_at,
       playlists.created_by_user_id,
       playlists.modified_at,
       playlists.modified_by_user_id,
       playlists.library_id,
       playlists.storage_path,
       playlists.external_id,
       playlists.external_url,
       playlists.name,
       playlists.refreshed_at,
       playlists.subscribed_at,
       playlists.video_count,
       playlists.total_count,

       image_files.id,
       image_files.public_id,
       image_files.created_at,
       image_files.created_by_user_id,
       image_files.modified_at,
       image_files.modified_by_user_id,
       image_files.storage_path,
       image_files.type,
       image_files.width,
       image_files.height,
       image_files.hash_algorithm,
       image_files.hash,
       image_files.storage_size
FROM filtered playlists
    LEFT JOIN media.playlist_images ON playlist_images.playlist_id = playlists.id
    LEFT JOIN media.image_files ON playlist_images.image_id = image_files.id
ORDER BY
         CASE WHEN @SortKey = 'random' THEN random() END,
         CASE WHEN @SortKey = 'created_at' AND NOT @SortDescending THEN playlists.created_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'created_at' AND @SortDescending THEN playlists.created_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'modified_at' AND NOT @SortDescending THEN playlists.modified_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'modified_at' AND @SortDescending THEN playlists.modified_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'subscribed_at' AND NOT @SortDescending THEN playlists.subscribed_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'subscribed_at' AND @SortDescending THEN playlists.subscribed_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'name' AND NOT @SortDescending THEN playlists.name END ASC NULLS LAST,
         CASE WHEN @SortKey = 'name' AND @SortDescending THEN playlists.name END DESC NULLS LAST,
         CASE WHEN @SortKey = 'refreshed_at' AND NOT @SortDescending THEN playlists.refreshed_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'refreshed_at' AND @SortDescending THEN playlists.refreshed_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'video_count' AND NOT @SortDescending THEN playlists.video_count END ASC NULLS LAST,
         CASE WHEN @SortKey = 'video_count' AND @SortDescending THEN playlists.video_count END DESC NULLS LAST,
         playlists.id;
