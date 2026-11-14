WITH parameters AS MATERIALIZED (SELECT websearch_to_tsquery('english', @Query) AS query),

    filtered AS (
        SELECT id,
               public_id,
               created_at,
               created_by_user_id,
               modified_at,
               modified_by_user_id,
               owner_id,
               name,
               (SELECT count(*)
                FROM media.creator_channels
                WHERE creator_channels.creator_id = creators.id) AS channel_count,
               count(*) OVER()                                   AS total_count
        FROM media.creators
            CROSS JOIN parameters
        WHERE (creators.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
          AND (@LibraryId IS NULL OR EXISTS(SELECT 1
                                            FROM media.library_channels
                                                INNER JOIN media.creator_channels ON library_channels.channel_id = creator_channels.channel_id
                                            WHERE library_id = (SELECT libraries.id FROM media.libraries WHERE libraries.public_id = @LibraryId) AND creator_id = creators.id))
          AND (@Query IS NULL OR creators.name @@ query)
        ORDER BY
                 CASE WHEN @SortKey = 'random' THEN random() END,
                 CASE WHEN @SortKey = 'created_at' AND NOT @SortDescending THEN creators.created_at END ASC NULLS LAST,
                 CASE WHEN @SortKey = 'created_at' AND @SortDescending THEN creators.created_at END DESC NULLS LAST,
                 CASE WHEN @SortKey = 'modified_at' AND NOT @SortDescending THEN creators.modified_at END ASC NULLS LAST,
                 CASE WHEN @SortKey = 'modified_at' AND @SortDescending THEN creators.modified_at END DESC NULLS LAST,
                 CASE WHEN @SortKey = 'channel_count' AND NOT @SortDescending THEN (SELECT count(*) FROM media.creator_channels WHERE creator_channels.creator_id = creators.id) END ASC NULLS LAST,
                 CASE WHEN @SortKey = 'channel_count' AND @SortDescending THEN (SELECT count(*) FROM media.creator_channels WHERE creator_channels.creator_id = creators.id) END DESC NULLS LAST,
                 CASE WHEN @SortKey = 'name' AND NOT @SortDescending THEN creators.name END ASC NULLS LAST,
                 CASE WHEN @SortKey = 'name' AND @SortDescending THEN creators.name END DESC NULLS LAST,
                 creators.id
        LIMIT @Limit
        OFFSET @Offset)

SELECT creators.id,
       creators.public_id,
       creators.created_at,
       creators.created_by_user_id,
       creators.modified_at,
       creators.modified_by_user_id,
       creators.owner_id,
       creators.name,
       creators.channel_count,
       creators.total_count,

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
FROM filtered creators
    LEFT JOIN media.creator_channels ON creator_channels.creator_id = creators.id AND creator_channels."primary"
    LEFT JOIN media.channel_images ON creator_channels.channel_id = channel_images.channel_id
    LEFT JOIN media.image_files ON channel_images.image_id = image_files.id
ORDER BY
         CASE WHEN @SortKey = 'random' THEN random() END,
         CASE WHEN @SortKey = 'created_at' AND NOT @SortDescending THEN creators.created_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'created_at' AND @SortDescending THEN creators.created_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'modified_at' AND NOT @SortDescending THEN creators.modified_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'modified_at' AND @SortDescending THEN creators.modified_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'channel_count' AND NOT @SortDescending THEN creators.channel_count END ASC NULLS LAST,
         CASE WHEN @SortKey = 'channel_count' AND @SortDescending THEN creators.channel_count END DESC NULLS LAST,
         CASE WHEN @SortKey = 'name' AND NOT @SortDescending THEN creators.name END ASC NULLS LAST,
         CASE WHEN @SortKey = 'name' AND @SortDescending THEN creators.name END DESC NULLS LAST,
         creators.id;
