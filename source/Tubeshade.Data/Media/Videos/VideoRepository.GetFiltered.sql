WITH parameters AS MATERIALIZED (SELECT websearch_to_tsquery('english', @Query) AS query)

SELECT videos.id,
       videos.created_at,
       videos.created_by_user_id,
       videos.modified_at,
       videos.modified_by_user_id,
       videos.owner_id,
       videos.channel_id,
       videos.storage_path,
       videos.external_id,
       videos.external_url,
       videos.name,
       videos.description,
       videos.categories,
       videos.tags,
       videos.published_at,
       videos.refreshed_at,
       videos.availability,
       videos.duration,
       videos.view_count,
       videos.like_count,
       videos.ignored_at,
       videos.ignored_by_user_id,
       videos.type,
       viewed.viewed,
       viewed.position,
       playlist_videos."order" AS playlist_order,
       count(*) OVER() AS TotalCount
FROM media.videos
    LEFT OUTER JOIN media.video_viewed_by_users viewed ON videos.id = viewed.video_id AND viewed.user_id = @UserId
    LEFT OUTER JOIN media.playlist_videos ON playlist_videos.video_id = videos.id AND playlist_videos.playlist_id = @PlaylistId
    CROSS JOIN parameters
    INNER JOIN media.channels ON videos.channel_id = channels.id
    INNER JOIN media.library_channels ON channels.id = library_channels.channel_id
WHERE (videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
  AND videos.ignored_at IS NULL
  AND ((@WithFiles = TRUE AND EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND downloaded_at IS NOT NULL)) OR
       (@WithFiles = FALSE AND NOT EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id)) OR
       (@WithFiles IS NULL AND
        (
            EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND downloaded_at IS NOT NULL) OR
            NOT EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id)
        )
       )
      )
  AND (@LibraryId IS NULL OR library_channels.library_id = @LibraryId)
  AND (@CreatorId IS NULL OR EXISTS(SELECT 1 FROM media.creator_channels WHERE creator_channels.channel_id = videos.channel_id AND creator_id = @CreatorId))
  AND (@ChannelId IS NULL OR library_channels.channel_id = @ChannelId)
  AND (@PlaylistId IS NULL OR playlist_videos."order" IS NOT NULL)
  AND (@Query IS NULL OR videos.searchable_index_value @@ query)
  AND (@Type::media.video_type IS NULL OR videos.type = @Type)
  AND (@Viewed::media.view_status IS NULL
           OR (@Viewed = 'viewed' AND viewed.viewed = TRUE)
           OR (@Viewed = 'not_viewed' AND (viewed.viewed IS NULL OR viewed.viewed = FALSE))
           OR (@Viewed = 'partially_viewed' AND viewed.viewed IS FALSE AND viewed.position > 10 AND viewed.position < EXTRACT(EPOCH FROM (videos.duration - '10 seconds'::interval))))
  AND (@Availability::media.external_availability IS NULL OR videos.availability = @Availability)
ORDER BY
         CASE WHEN @SortKey = 'random' THEN random() END,
         CASE WHEN @SortKey = 'created_at' AND NOT @SortDescending THEN videos.created_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'created_at' AND @SortDescending THEN videos.created_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'modified_at' AND NOT @SortDescending THEN videos.modified_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'modified_at' AND @SortDescending THEN videos.modified_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'published_at' AND NOT @SortDescending THEN videos.published_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'published_at' AND @SortDescending THEN videos.published_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'refreshed_at' AND NOT @SortDescending THEN videos.refreshed_at END ASC NULLS LAST,
         CASE WHEN @SortKey = 'refreshed_at' AND @SortDescending THEN videos.refreshed_at END DESC NULLS LAST,
         CASE WHEN @SortKey = 'view_count' AND NOT @SortDescending THEN videos.view_count END ASC NULLS LAST,
         CASE WHEN @SortKey = 'view_count' AND @SortDescending THEN videos.view_count END DESC NULLS LAST,
         CASE WHEN @SortKey = 'like_count' AND NOT @SortDescending THEN videos.like_count END ASC NULLS LAST,
         CASE WHEN @SortKey = 'like_count' AND @SortDescending THEN videos.like_count END DESC NULLS LAST,
         CASE WHEN @SortKey = 'downloaded_at' AND NOT @SortDescending THEN (SELECT MAX(video_files.downloaded_at) FROM media.video_files WHERE video_files.video_id = videos.id) END ASC NULLS LAST,
         CASE WHEN @SortKey = 'downloaded_at' AND @SortDescending THEN (SELECT MAX(video_files.downloaded_at) FROM media.video_files WHERE video_files.video_id = videos.id) END DESC NULLS LAST,
         CASE WHEN @SortKey = 'duration' AND NOT @SortDescending THEN videos.duration END ASC NULLS LAST,
         CASE WHEN @SortKey = 'duration' AND @SortDescending THEN videos.duration END DESC NULLS LAST,
         CASE WHEN @SortKey = 'storage_size' AND NOT @SortDescending THEN (SELECT SUM(video_files.storage_size) FROM media.video_files WHERE video_files.video_id = videos.id) END ASC NULLS LAST,
         CASE WHEN @SortKey = 'storage_size' AND @SortDescending THEN (SELECT SUM(video_files.storage_size) FROM media.video_files WHERE video_files.video_id = videos.id) END DESC NULLS LAST,
         CASE WHEN @SortKey = 'framerate' AND NOT @SortDescending THEN (SELECT MAX(video_files.framerate) FROM media.video_files WHERE video_files.video_id = videos.id) END ASC NULLS LAST,
         CASE WHEN @SortKey = 'framerate' AND @SortDescending THEN (SELECT MAX(video_files.framerate) FROM media.video_files WHERE video_files.video_id = videos.id) END DESC NULLS LAST,
         CASE WHEN @SortKey = 'query' AND NOT @SortDescending THEN ts_rank(videos.searchable_index_value, query) END ASC NULLS LAST,
         CASE WHEN @SortKey = 'query' AND @SortDescending THEN ts_rank(videos.searchable_index_value, query) END DESC NULLS LAST,
         CASE WHEN @SortKey = 'playlist_order' AND NOT @SortDescending THEN playlist_videos."order" END ASC NULLS LAST,
         CASE WHEN @SortKey = 'playlist_order' AND @SortDescending THEN playlist_videos."order" END DESC NULLS LAST,
         videos.id
LIMIT @Limit
OFFSET @Offset;
