WITH filtered AS (
    SELECT channels.id,
           channels.public_id,
           channels.created_at,
           channels.created_by_user_id,
           channels.modified_at,
           channels.modified_by_user_id,
           channels.owner_id,
           channels.name,
           channels.storage_path,
           channels.external_id,
           channels.subscribed_at,
           channels.subscriber_count,
           channels.external_url,
           channels.availability,
           (SELECT libraries.public_id
            FROM media.library_channels
                INNER JOIN media.libraries ON libraries.id = library_channels.library_id
            WHERE channel_id = channels.id AND library_channels."primary") AS primary_library_id,
           (SELECT count(*)
            FROM media.videos
            WHERE videos.channel_id = channels.id)                         AS video_count,
           count(*) OVER()                                                 AS total_count
    FROM media.channels
    WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
      AND (@Id IS NULL OR channels.public_id = @Id)
      AND (@LibraryId IS NULL OR EXISTS(SELECT 1 FROM media.library_channels WHERE library_id = (SELECT libraries.id FROM media.libraries WHERE libraries.public_id = @LibraryId) AND channel_id = channels.id))
      AND (@CreatorId IS NULL OR EXISTS(SELECT 1 FROM media.creator_channels WHERE creator_id = (SELECT creators.id FROM media.creators WHERE creators.public_id = @CreatorId) AND channel_id = channels.id))
      AND (@Query IS NULL OR channels.name @@ websearch_to_tsquery('english', @Query))
      AND (@Availability::media.external_availability IS NULL OR channels.availability = @Availability)
    ORDER BY
             CASE WHEN @SortKey = 'random' THEN random() END,
             CASE WHEN @SortKey = 'created_at' AND NOT @SortDescending THEN channels.created_at END ASC NULLS LAST,
             CASE WHEN @SortKey = 'created_at' AND @SortDescending THEN channels.created_at END DESC NULLS LAST,
             CASE WHEN @SortKey = 'modified_at' AND NOT @SortDescending THEN channels.modified_at END ASC NULLS LAST,
             CASE WHEN @SortKey = 'modified_at' AND @SortDescending THEN channels.modified_at END DESC NULLS LAST,
             CASE WHEN @SortKey = 'video_count' AND NOT @SortDescending THEN (SELECT count(*) FROM media.videos WHERE videos.channel_id = channels.id) END ASC NULLS LAST,
             CASE WHEN @SortKey = 'video_count' AND @SortDescending THEN (SELECT count(*) FROM media.videos WHERE videos.channel_id = channels.id) END DESC NULLS LAST,
             CASE WHEN @SortKey = 'subscriber_count' AND NOT @SortDescending THEN channels.subscriber_count END ASC NULLS LAST,
             CASE WHEN @SortKey = 'subscriber_count' AND @SortDescending THEN channels.subscriber_count END DESC NULLS LAST,
             CASE WHEN @SortKey = 'subscribed_at' AND NOT @SortDescending THEN channels.subscribed_at END ASC NULLS LAST,
             CASE WHEN @SortKey = 'subscribed_at' AND @SortDescending THEN channels.subscribed_at END DESC NULLS LAST,
             CASE WHEN @SortKey = 'name' AND NOT @SortDescending THEN channels.name END ASC NULLS LAST,
             CASE WHEN @SortKey = 'name' AND @SortDescending THEN channels.name END DESC NULLS LAST,
             channels.id
    LIMIT @Limit
    OFFSET @Offset)

SELECT channels.id,
       channels.public_id,
       channels.created_at,
       channels.created_by_user_id,
       channels.modified_at,
       channels.modified_by_user_id,
       channels.owner_id,
       channels.name,
       channels.storage_path,
       channels.external_id,
       channels.subscribed_at,
       channels.subscriber_count,
       channels.external_url,
       channels.availability,
       channels.primary_library_id,
       channels.video_count,
       channels.total_count,

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
FROM filtered channels
    LEFT JOIN media.channel_images ON channels.id = channel_images.channel_id
    LEFT JOIN media.image_files ON channel_images.image_id = image_files.id
ORDER BY
         CASE WHEN @SortKey = 'random' THEN random() END,
         CASE WHEN @SortKey = 'created_at' AND NOT @SortDescending THEN channels.created_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'created_at' AND @SortDescending THEN channels.created_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'modified_at' AND NOT @SortDescending THEN channels.modified_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'modified_at' AND @SortDescending THEN channels.modified_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'video_count' AND NOT @SortDescending THEN channels.video_count END ASC NULLS LAST,
         CASE WHEN @SortKey = 'video_count' AND @SortDescending THEN channels.video_count END DESC NULLS LAST,
         CASE WHEN @SortKey = 'subscriber_count' AND NOT @SortDescending THEN channels.subscriber_count END ASC NULLS LAST,
         CASE WHEN @SortKey = 'subscriber_count' AND @SortDescending THEN channels.subscriber_count END DESC NULLS LAST,
         CASE WHEN @SortKey = 'subscribed_at' AND NOT @SortDescending THEN channels.subscribed_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'subscribed_at' AND @SortDescending THEN channels.subscribed_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'name' AND NOT @SortDescending THEN channels.name END ASC NULLS LAST,
         CASE WHEN @SortKey = 'name' AND @SortDescending THEN channels.name END DESC NULLS LAST,
         channels.id;
