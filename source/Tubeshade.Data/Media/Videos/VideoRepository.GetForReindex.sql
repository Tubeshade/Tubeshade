WITH stale_videos AS (SELECT videos.id,
                             videos.public_id,
                             channels.public_id AS channel_id,
                             videos.published_at,
                             videos.external_url,
                             videos.refreshed_at - videos.published_at AS from_publish,
                             CURRENT_TIMESTAMP - videos.refreshed_at AS from_now
                      FROM media.videos
                          INNER JOIN media.channels ON channels.id = videos.channel_id
                          INNER JOIN media.library_channels ON videos.channel_id = library_channels.channel_id
                      WHERE library_channels."primary"
                        AND library_channels.library_id = (SELECT libraries.id FROM media.libraries WHERE libraries.public_id = @LibraryId)
                        AND videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
                        AND videos.ignored_at IS NULL
                        AND videos.published_at < CURRENT_TIMESTAMP)

SELECT public_id,
       channel_id,
       external_url
FROM stale_videos
WHERE ((from_publish <= 'PT1H' AND from_now >= 'PT30M') OR
       (from_publish <= 'P2D' AND from_now >= 'P1D') OR
       (from_publish <= 'P2M' AND from_now >= 'P1M') OR
       from_now >= 'P6M')
ORDER BY stale_videos.published_at DESC, stale_videos.id
LIMIT 5;
