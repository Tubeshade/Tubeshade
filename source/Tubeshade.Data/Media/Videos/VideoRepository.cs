using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Data.Media.Videos;

public sealed class VideoRepository(NpgsqlConnection connection) : ModifiableRepositoryBase<VideoEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.videos";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO media.videos (created_by_user_id, modified_by_user_id, owner_id, name, description, categories, tags, type, view_count, like_count, channel_id, storage_path, external_id, external_url, published_at, refreshed_at, availability, duration, ignored_at, ignored_by_user_id) 
         VALUES (@CreatedByUserId, @ModifiedByUserId, @OwnerId, @Name, @Description, @Categories, @Tags, @Type, @ViewCount, @LikeCount, @ChannelId, @StoragePath, @ExternalId, @ExternalUrl, @PublishedAt, @RefreshedAt, @Availability, @Duration, @IgnoredAt, @IgnoredByUserId)
         RETURNING id;
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        $"""
         SELECT videos.id,
                videos.created_at,
                videos.created_by_user_id,
                videos.modified_at,
                videos.modified_by_user_id,
                videos.owner_id,
                videos.name,
                videos.description,
                videos.categories,
                videos.tags,
                videos.type,
                videos.view_count,
                videos.like_count,
                videos.channel_id,
                videos.storage_path,
                videos.external_id,
                videos.external_url,
                videos.published_at,
                videos.refreshed_at,
                videos.availability,
                videos.duration,
                videos.ignored_at,
                videos.ignored_by_user_id,
                video_viewed_by_users.viewed,
                video_viewed_by_users.position
         FROM media.videos
           LEFT OUTER JOIN media.video_viewed_by_users ON videos.id = video_viewed_by_users.video_id AND video_viewed_by_users.user_id = @{nameof(GetParameters.UserId)}
         """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        $"""
             name = @{nameof(VideoEntity.Name)},
             channel_id = @{nameof(VideoEntity.ChannelId)},
             storage_path = @{nameof(VideoEntity.StoragePath)},
             external_id = @{nameof(VideoEntity.ExternalId)},
             published_at = @{nameof(VideoEntity.PublishedAt)},
             refreshed_at = @{nameof(VideoEntity.RefreshedAt)},
             availability = @{nameof(VideoEntity.Availability)},
             duration = @{nameof(VideoEntity.Duration)},
             description = @{nameof(VideoEntity.Description)},
             categories = @{nameof(VideoEntity.Categories)},
             tags = @{nameof(VideoEntity.Tags)},
             view_count = @{nameof(VideoEntity.ViewCount)},
             like_count = @{nameof(VideoEntity.LikeCount)},
             ignored_at = @{nameof(VideoEntity.IgnoredAt)},
             ignored_by_user_id = @{nameof(VideoEntity.IgnoredByUserId)}
         """;

    private string SelectFilesSql =>
        // lang=sql
        $"""
         {AccessCte}

         SELECT video_files.id,
                video_files.created_at,
                video_files.created_by_user_id,
                video_files.modified_at,
                video_files.modified_by_user_id,
                video_files.owner_id,
                video_files.video_id,
                video_files.storage_path,
                video_files.type,
                video_files.width,
                video_files.height,
                video_files.framerate,
                video_files.downloaded_at,
                video_files.downloaded_by_user_id,
                video_files.hash_algorithm,
                video_files.hash,
                video_files.storage_size,
                downloading.task_run_id,
                downloading.path AS TempPath
         FROM media.video_files
            INNER JOIN media.videos ON video_files.video_id = videos.id
            LEFT OUTER JOIN media.video_files_downloading downloading ON video_files.id = downloading.file_id
         WHERE {AccessFilter} AND videos.id = @{nameof(GetVideoParameters.VideoId)}
         ORDER BY video_files.width DESC, video_files.framerate DESC, video_files.id;
         """;

    public async ValueTask<List<DetailedVideo>> GetDownloadableVideos(
        VideoParameters parameters,
        CancellationToken cancellationToken = default)
    {
        parameters.Downloadable = true;
        var query = GetFilteredDetailedVideosQuery(parameters);

        var command = new CommandDefinition(query, parameters, cancellationToken: cancellationToken);
        return await GetDetailed(command);
    }
    public async ValueTask<List<DetailedVideo>> GetFilteredDetailed(
        VideoParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = GetFilteredDetailedVideosQuery(parameters);
        var command = new CommandDefinition(query, parameters, cancellationToken: cancellationToken);
        return await GetDetailed(command);
    }

    public async ValueTask<List<VideoEntity>> GetFiltered(
        VideoParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = GetFilteredVideosQuery(parameters);
        var command = new CommandDefinition(query, parameters, cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoEntity>(command);
        return enumerable as List<VideoEntity> ?? enumerable.ToList();
    }

    public async ValueTask<VideoEntity?> GetLatestDownloadedVideo(
        Guid userId,
        Guid libraryId,
        Guid channelId,
        NpgsqlTransaction transaction)
    {
        var parameters = new VideoParameters
        {
            UserId = userId,
            LibraryId = libraryId,
            ChannelId = channelId,
            Limit = 1,
            Offset = 0,
            WithFiles = true,
            SortBy = SortVideoBy.PublishedAt,
            SortDirection = SortDirection.Descending,
        };

        var query = GetFilteredVideosQuery(parameters);
        var command = new CommandDefinition(query, parameters, transaction);
        return await Connection.QuerySingleOrDefaultAsync<VideoEntity>(command);
    }

    public async ValueTask<List<(Guid VideoId, Guid ChannelId, string VideoUrl)>> GetForReindex(
        Guid libraryId,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             WITH stale_videos AS (SELECT videos.id,
                                          videos.channel_id,
                                          videos.published_at,
                                          external_url,
                                          refreshed_at - published_at AS from_publish,
                                          CURRENT_TIMESTAMP - refreshed_at AS from_now
                                   FROM media.videos
                                       INNER JOIN media.library_channels ON videos.channel_id = library_channels.channel_id
                                   WHERE library_channels."primary" 
                                     AND library_channels.library_id = @{nameof(libraryId)}
                                     AND videos.ignored_at IS NULL
                                     AND videos.published_at < CURRENT_TIMESTAMP)

             SELECT id,
                    channel_id,
                    external_url
             FROM stale_videos
             WHERE ((from_publish <= 'PT1H' AND from_now >= 'PT30M') OR
                    (from_publish <= 'P2D' AND from_now >= 'P1D') OR
                    (from_publish <= 'P2M' AND from_now >= 'P1M') OR
                    from_now >= 'P6M') 
             ORDER BY stale_videos.published_at DESC, stale_videos.id
             LIMIT 5;
             """,
            new { libraryId },
            transaction);

        var enumerable = await Connection.QueryAsync<(Guid, Guid, string)>(command);
        return enumerable as List<(Guid, Guid, string)> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoFileEntity>> GetFilesAsync(
        Guid videoId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            SelectFilesSql,
            new GetVideoParameters(videoId, userId, Access.Read),
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoFileEntity>(command);
        return enumerable as List<VideoFileEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoFileEntity>> GetFilesAsync(
        Guid videoId,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            SelectFilesSql,
            new GetVideoParameters(videoId, userId, Access.Read),
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoFileEntity>(command);
        return enumerable as List<VideoFileEntity> ?? enumerable.ToList();
    }

    public async ValueTask<VideoFileEntity?> FindFileAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             {AccessCte}

             SELECT video_files.id,
                    video_files.created_at,
                    video_files.created_by_user_id,
                    video_files.modified_at,
                    video_files.modified_by_user_id,
                    video_files.owner_id,
                    video_files.video_id,
                    video_files.storage_path,
                    video_files.type,
                    video_files.width,
                    video_files.height,
                    video_files.framerate,
                    video_files.downloaded_at,
                    video_files.downloaded_by_user_id,
                    video_files.hash_algorithm,
                    video_files.hash,
                    video_files.storage_size,
                    downloading.task_run_id,
                    downloading.path AS TempPath
             FROM media.video_files
                INNER JOIN media.videos ON video_files.video_id = videos.id
                LEFT OUTER JOIN media.video_files_downloading downloading ON video_files.id = downloading.file_id
             WHERE {AccessFilter} AND video_files.id = @{nameof(GetSingleParameters.Id)};
             """,
            new GetSingleParameters(id, userId, Access.Read),
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleOrDefaultAsync<VideoFileEntity>(command);
    }

    public async ValueTask<VideoEntity?> FindByExternalId(
        string externalId,
        Guid userId,
        Access access,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            $"""
             {SelectAccessibleSql}
               AND {TableName}.external_id = @{nameof(GetSingleExternalParameters.ExternalId)};
             """,
            new GetSingleExternalParameters(externalId, userId, access),
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<VideoEntity>(command);
    }

    public async ValueTask<List<EntityId>> FindByExternalIds(
        List<string> externalIds,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             {AccessCte}

             SELECT videos.id,
                    videos.external_id
             FROM media.videos
             WHERE {AccessFilter}
               AND (videos.external_id = ANY(@{nameof(externalIds)}));
             """,
            new { UserId = userId, externalIds, Access = Access.Read },
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<EntityId>(command);
        return enumerable as List<EntityId> ?? enumerable.ToList();
    }

    public async ValueTask<VideoEntity?> FindByExternalUrl(string externalUrl, Guid userId, Access access,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            $"""
             {SelectAccessibleSql}
               AND {TableName}.external_url = @{nameof(GetSingleExternalUrlParameters.ExternalUrl)};
             """,
            new GetSingleExternalUrlParameters(externalUrl, userId, access),
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<VideoEntity>(command);
    }

    public async ValueTask<List<EntityId>> GetWithoutSegments(
        Guid userId,
        Guid libraryId,
        NpgsqlTransaction transaction)
    {
        var enumerable = await Connection.QueryAsync<EntityId>(
            $"""
             SELECT videos.id,
                    videos.external_id
             FROM media.videos
                      INNER JOIN media.channels ON videos.channel_id = channels.id
                      INNER JOIN media.library_channels ON channels.id = library_channels.channel_id
                      LEFT OUTER JOIN media.sponsorblock_segments ON videos.id = sponsorblock_segments.video_id
             WHERE library_id = @{nameof(GetFromLibraryParameters.LibraryId)}
               AND sponsorblock_segments.video_id IS NULL;
             """,
            new GetFromLibraryParameters(userId, libraryId, Access.Read),
            transaction);

        return enumerable as List<EntityId> ?? enumerable.ToList();
    }

    public async ValueTask<List<EntityId>> GetWithUnlockedSegments(
        Guid userId,
        Guid libraryId,
        NpgsqlTransaction transaction)
    {
        var enumerable = await Connection.QueryAsync<EntityId>(
            $"""
             SELECT videos.id,
                    videos.external_id
             FROM media.videos
                      INNER JOIN media.channels ON videos.channel_id = channels.id
                      INNER JOIN media.library_channels ON channels.id = library_channels.channel_id
                      INNER JOIN media.sponsorblock_segments ON videos.id = sponsorblock_segments.video_id
             WHERE library_id = @{nameof(GetFromLibraryParameters.LibraryId)}
             GROUP BY videos.id, videos.external_id
             HAVING bool_and(sponsorblock_segments.locked) IS NOT TRUE;
             """,
            new GetFromLibraryParameters(userId, libraryId, Access.Read),
            transaction);

        return enumerable as List<EntityId> ?? enumerable.ToList();
    }

    public async ValueTask<int> MarkAsWatched(Guid videoId, Guid userId, NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            $"""
             INSERT INTO media.video_viewed_by_users (video_id, user_id, viewed)
             VALUES (@{nameof(videoId)}, @{nameof(userId)}, true)
             ON CONFLICT (video_id, user_id) DO UPDATE
             SET modified_at = CURRENT_TIMESTAMP,
                 viewed = true;
             """,
            new { videoId, userId },
            transaction);
    }

    public async ValueTask<int> MarkAsNotWatched(Guid videoId, Guid userId, NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            $"""
             UPDATE media.video_viewed_by_users
             SET modified_at = CURRENT_TIMESTAMP,
                 viewed = false
             WHERE video_viewed_by_users.video_id = @{nameof(videoId)}
               AND video_viewed_by_users.user_id = @{nameof(userId)};
             """,
            new { videoId, userId },
            transaction);
    }

    public async ValueTask<int> UpdatePlaybackPosition(Guid videoId, Guid userId, double position,
        NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            $"""
             INSERT INTO media.video_viewed_by_users (video_id, user_id, viewed)
             VALUES (@{nameof(videoId)}, @{nameof(userId)}, false)
             ON CONFLICT DO NOTHING;         

             UPDATE media.video_viewed_by_users
             SET modified_at = CURRENT_TIMESTAMP,
                 position = @{nameof(position)}
             WHERE video_viewed_by_users.video_id = @{nameof(videoId)}
               AND video_viewed_by_users.user_id = @{nameof(userId)};
             """,
            new { videoId, userId, position },
            transaction);
    }

    private static string GetFilteredVideosQuery(VideoParameters parameters) =>
        // lang=sql
        $"""
         WITH accessible AS
             (SELECT videos.id
              FROM media.videos
                  INNER JOIN identity.owners ON owners.id = videos.owner_id
                  INNER JOIN identity.ownerships ON
                      ownerships.owner_id = owners.id AND
                      ownerships.user_id = @{nameof(parameters.UserId)} AND
                      (ownerships.access = @{nameof(parameters.Access)} OR ownerships.access = 'owner')),

             parameters AS MATERIALIZED (SELECT websearch_to_tsquery('english', @{nameof(parameters.Query)}) AS query)

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
                count(*) OVER() AS {nameof(VideoEntity.TotalCount)}
         FROM media.videos
             LEFT OUTER JOIN media.video_viewed_by_users viewed ON videos.id = viewed.video_id AND viewed.user_id = @{nameof(parameters.UserId)}
             CROSS JOIN parameters
             INNER JOIN media.channels ON videos.channel_id = channels.id
             INNER JOIN media.library_channels ON channels.id = library_channels.channel_id
         WHERE (videos.id IN (SELECT id FROM accessible))
           AND videos.ignored_at IS NULL
           AND ((@{nameof(parameters.WithFiles)} = TRUE AND EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND downloaded_at IS NOT NULL)) OR
                (@{nameof(parameters.WithFiles)} = FALSE AND NOT EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id)) OR
                (@{nameof(parameters.WithFiles)} IS NULL AND
                 (
                     EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND downloaded_at IS NOT NULL) OR
                     NOT EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id)
                 )
                )
               )
           AND (@{nameof(parameters.LibraryId)} IS NULL OR library_channels.library_id = @{nameof(parameters.LibraryId)})
           AND (@{nameof(parameters.ChannelId)} IS NULL OR library_channels.channel_id = @{nameof(parameters.ChannelId)})
           AND (@{nameof(parameters.Query)} IS NULL OR videos.searchable_index_value @@ query)
           AND (@{nameof(parameters.Type)}::media.video_type IS NULL OR videos.type = @{nameof(parameters.Type)})
           AND (@{nameof(parameters.Viewed)}::media.view_status IS NULL
                    OR (@{nameof(parameters.Viewed)} = '{ViewStatus.Names.Viewed}' AND viewed.viewed = TRUE)
                    OR (@{nameof(parameters.Viewed)} = '{ViewStatus.Names.NotViewed}' AND (viewed.viewed IS NULL OR viewed.viewed = FALSE))
                    OR (@{nameof(parameters.Viewed)} = '{ViewStatus.Names.PartiallyViewed}' AND viewed.viewed IS FALSE AND viewed.position > 10 AND viewed.position < EXTRACT(EPOCH FROM (videos.duration - '10 seconds'::interval))))
           AND (@{nameof(parameters.Availability)}::media.external_availability IS NULL OR videos.availability = @{nameof(parameters.Availability)})
         ORDER BY {parameters.SortBy.SortExpression} {parameters.SortDirection.Name} NULLS LAST, videos.id
         LIMIT @{nameof(parameters.Limit)}
         OFFSET @{nameof(parameters.Offset)};
         """;

    private static string GetFilteredDetailedVideosQuery(VideoParameters parameters) =>
        // lang=sql
        $"""
         WITH accessible AS
             (SELECT videos.id
              FROM media.videos
                  INNER JOIN identity.owners ON owners.id = videos.owner_id
                  INNER JOIN identity.ownerships ON
                      ownerships.owner_id = owners.id AND
                      ownerships.user_id = @{nameof(parameters.UserId)} AND
                      (ownerships.access = @{nameof(parameters.Access)} OR ownerships.access = 'owner')),

             downloading AS MATERIALIZED
                 (SELECT tasks.video_id
                  FROM tasks.tasks
                      INNER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
                      LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
                  WHERE tasks.type = '{TaskType.Names.DownloadVideo}'
                    AND task_run_results.id IS NULL),
         
             parameters AS MATERIALIZED (SELECT websearch_to_tsquery('english', @{nameof(parameters.Query)}) AS query),

             filtered AS
                 (SELECT videos.id,
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
                         count(*) OVER() AS count,
                         videos.searchable_index_value AS searchable_index_value
                  FROM media.videos
                      LEFT OUTER JOIN media.video_viewed_by_users viewed ON videos.id = viewed.video_id AND viewed.user_id = @{nameof(parameters.UserId)}
                      CROSS JOIN parameters
                      INNER JOIN media.channels ON videos.channel_id = channels.id
                      INNER JOIN media.library_channels ON channels.id = library_channels.channel_id
                  WHERE (videos.id IN (SELECT id FROM accessible))
                    AND videos.ignored_at IS NULL
                    AND (@{nameof(parameters.Downloadable)} = FALSE OR NOT EXISTS(SELECT 1 FROM downloading WHERE downloading.video_id = videos.id))
                    AND ((@{nameof(parameters.WithFiles)} = TRUE AND EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND (downloaded_at IS NULL = @{nameof(parameters.Downloadable)}))) OR
                         (@{nameof(parameters.WithFiles)} = FALSE AND NOT EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id)) OR
                         (@{nameof(parameters.WithFiles)} IS NULL AND
                          (
                              EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND (downloaded_at IS NULL = @{nameof(parameters.Downloadable)})) OR
                              NOT EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id)
                          )
                         )
                        )
                    AND (@{nameof(parameters.LibraryId)} IS NULL OR library_channels.library_id = @{nameof(parameters.LibraryId)})
                    AND (@{nameof(parameters.ChannelId)} IS NULL OR library_channels.channel_id = @{nameof(parameters.ChannelId)})
                    AND (@{nameof(parameters.Query)} IS NULL OR videos.searchable_index_value @@ query)
                    AND (@{nameof(parameters.Type)}::media.video_type IS NULL OR videos.type = @{nameof(parameters.Type)})
                    AND (@{nameof(parameters.Viewed)}::media.view_status IS NULL
                             OR (@{nameof(parameters.Viewed)} = '{ViewStatus.Names.Viewed}' AND viewed.viewed = TRUE)
                             OR (@{nameof(parameters.Viewed)} = '{ViewStatus.Names.NotViewed}' AND (viewed.viewed IS NULL OR viewed.viewed = FALSE))
                             OR (@{nameof(parameters.Viewed)} = '{ViewStatus.Names.PartiallyViewed}' AND viewed.viewed IS FALSE AND viewed.position > 10 AND viewed.position < EXTRACT(EPOCH FROM (videos.duration - '10 seconds'::interval))))
                    AND (@{nameof(parameters.Availability)}::media.external_availability IS NULL OR videos.availability = @{nameof(parameters.Availability)})
                  ORDER BY {parameters.SortBy.SortExpression} {parameters.SortDirection.Name} NULLS LAST, videos.id
                  LIMIT @{nameof(parameters.Limit)}
                  OFFSET @{nameof(parameters.Offset)})

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
                videos.viewed,
                videos.position,
                videos.count AS {nameof(VideoEntity.TotalCount)},

                image_files.id,
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
         FROM filtered videos
             CROSS JOIN parameters
             LEFT JOIN media.video_images ON videos.id = video_images.video_id
             LEFT JOIN media.image_files ON video_images.image_id = image_files.id
         ORDER BY {parameters.SortBy.SortExpression} {parameters.SortDirection.Name} NULLS LAST, videos.id;
         """;

    private async ValueTask<List<DetailedVideo>> GetDetailed(CommandDefinition command)
    {
        var enumerable = await Connection.QueryAsync<DetailedVideo, ImageFileEntity?, DetailedVideo>(command, MapSplitRow);

        return enumerable
            .GroupBy(video => video.Id)
            .Select(grouping =>
            {
                var video = grouping.First();
                video.Thumbnails = grouping.SelectMany(channel => channel.Thumbnails).ToArray();
                return video;
            })
            .ToList();
    }

    private static DetailedVideo MapSplitRow(DetailedVideo video, ImageFileEntity? image)
    {
        if (image is not null)
        {
            video.Thumbnails = [image];
        }

        return video;
    }
}
