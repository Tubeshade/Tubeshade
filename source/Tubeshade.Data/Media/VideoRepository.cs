using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media;

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
         SELECT videos.id AS {nameof(VideoEntity.Id)},
                videos.created_at AS {nameof(VideoEntity.CreatedAt)},
                videos.created_by_user_id AS {nameof(VideoEntity.CreatedByUserId)},
                videos.modified_at AS {nameof(VideoEntity.ModifiedAt)},
                videos.modified_by_user_id AS {nameof(VideoEntity.ModifiedByUserId)},
                videos.owner_id AS {nameof(VideoEntity.OwnerId)},
                videos.name AS {nameof(VideoEntity.Name)},
                videos.description AS {nameof(VideoEntity.Description)},
                videos.categories AS {nameof(VideoEntity.Categories)},
                videos.tags AS {nameof(VideoEntity.Tags)},
                videos.type AS {nameof(VideoEntity.Type)},
                videos.view_count AS {nameof(VideoEntity.ViewCount)},
                videos.like_count AS {nameof(VideoEntity.LikeCount)},
                videos.channel_id AS {nameof(VideoEntity.ChannelId)},
                videos.storage_path AS {nameof(VideoEntity.StoragePath)},
                videos.external_id AS {nameof(VideoEntity.ExternalId)},
                videos.external_url AS {nameof(VideoEntity.ExternalUrl)},
                videos.published_at AS PublishedAt,
                videos.refreshed_at AS RefreshedAt,
                videos.availability AS Availability,
                videos.duration AS Duration,
                videos.ignored_at AS IgnoredAt,
                videos.ignored_by_user_id AS IgnoredByUserId,
                video_viewed_by_users.viewed AS Viewed,
                video_viewed_by_users.position AS Position
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

    public async ValueTask<List<VideoEntity>> GetDownloadableVideos(
        VideoParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             {AccessCte},
                  downloading AS
                  (SELECT tasks.video_id
                   FROM tasks.tasks
                            INNER JOIN tasks.task_runs ON tasks.id = task_runs.task_id
                            LEFT OUTER JOIN tasks.task_run_results ON task_runs.id = task_run_results.run_id
                   WHERE tasks.type = 'download_video'
                     AND task_run_results.id IS NULL)

             SELECT videos.id AS {nameof(VideoEntity.Id)},
                    videos.created_at AS {nameof(VideoEntity.CreatedAt)},
                    videos.created_by_user_id AS {nameof(VideoEntity.CreatedByUserId)},
                    videos.modified_at AS {nameof(VideoEntity.ModifiedAt)},
                    videos.modified_by_user_id AS {nameof(VideoEntity.ModifiedByUserId)},
                    videos.owner_id AS {nameof(VideoEntity.OwnerId)},
                    videos.name AS {nameof(VideoEntity.Name)},
                    videos.type AS {nameof(VideoEntity.Type)},
                    videos.channel_id AS {nameof(VideoEntity.ChannelId)},
                    videos.storage_path AS {nameof(VideoEntity.StoragePath)},
                    videos.external_id AS {nameof(VideoEntity.ExternalId)},
                    videos.external_url AS {nameof(VideoEntity.ExternalUrl)},
                    videos.published_at AS PublishedAt,
                    videos.refreshed_at AS RefreshedAt,
                    videos.availability AS Availability,
                    videos.duration AS Duration,
                    count(*) OVER() AS {nameof(VideoEntity.TotalCount)}
             FROM media.videos
                INNER JOIN media.channels ON videos.channel_id = channels.id
                INNER JOIN media.library_channels ON channels.id = library_channels.channel_id
             WHERE {AccessFilter}
               AND videos.ignored_at IS NULL
               AND NOT EXISTS(SELECT 1 FROM downloading WHERE downloading.video_id = videos.id)
               AND ((@{nameof(parameters.WithFiles)} = TRUE AND EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND downloaded_at IS NULL)) OR
                    (@{nameof(parameters.WithFiles)} = FALSE AND NOT EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id)) OR
                    (@{nameof(parameters.WithFiles)} IS NULL AND
                     (
                        EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND downloaded_at IS NULL) OR
                        NOT EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id)
                     )
                    )
                   )
               AND (@{nameof(parameters.LibraryId)} IS NULL OR library_channels.library_id = @{nameof(parameters.LibraryId)})
               AND (@{nameof(parameters.ChannelId)} IS NULL OR videos.channel_id = @{nameof(parameters.ChannelId)})
               AND (@{nameof(parameters.Query)} IS NULL OR videos.searchable_index_value @@ websearch_to_tsquery('english', @{nameof(parameters.Query)}))
               AND (@{nameof(parameters.Type)}::media.video_type IS NULL OR videos.type = @{nameof(parameters.Type)})
               AND (@{nameof(parameters.Availability)}::media.external_availability IS NULL OR videos.availability = @{nameof(parameters.Availability)})
             ORDER BY videos.published_at DESC
             LIMIT @{nameof(parameters.Limit)}
             OFFSET @{nameof(parameters.Offset)};
             """,
            parameters,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoEntity>(command);
        return enumerable as List<VideoEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoEntity>> GetFiltered(
        VideoParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             {AccessCte}

             SELECT videos.id AS {nameof(VideoEntity.Id)},
                    videos.created_at AS {nameof(VideoEntity.CreatedAt)},
                    videos.created_by_user_id AS {nameof(VideoEntity.CreatedByUserId)},
                    videos.modified_at AS {nameof(VideoEntity.ModifiedAt)},
                    videos.modified_by_user_id AS {nameof(VideoEntity.ModifiedByUserId)},
                    videos.owner_id AS {nameof(VideoEntity.OwnerId)},
                    videos.name AS {nameof(VideoEntity.Name)},
                    videos.type AS {nameof(VideoEntity.Type)},
                    videos.channel_id AS {nameof(VideoEntity.ChannelId)},
                    videos.storage_path AS {nameof(VideoEntity.StoragePath)},
                    videos.external_id AS {nameof(VideoEntity.ExternalId)},
                    videos.external_url AS {nameof(VideoEntity.ExternalUrl)},
                    videos.published_at AS {nameof(VideoEntity.PublishedAt)},
                    videos.refreshed_at AS {nameof(VideoEntity.RefreshedAt)},
                    videos.availability AS {nameof(VideoEntity.Availability)},
                    videos.duration AS {nameof(VideoEntity.Duration)},
                    video_viewed_by_users.viewed AS {nameof(VideoEntity.Viewed)},
                    video_viewed_by_users.position AS {nameof(VideoEntity.Position)},
                    count(*) OVER() AS {nameof(VideoEntity.TotalCount)}
             FROM media.videos
                LEFT OUTER JOIN media.video_viewed_by_users ON videos.id = video_viewed_by_users.video_id AND video_viewed_by_users.user_id = @{nameof(parameters.UserId)}
                INNER JOIN media.channels ON videos.channel_id = channels.id
                INNER JOIN media.library_channels ON channels.id = library_channels.channel_id
             WHERE {AccessFilter} 
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
               AND (@{nameof(parameters.Query)} IS NULL OR videos.searchable_index_value @@ websearch_to_tsquery('english', @{nameof(parameters.Query)}))
               AND (@{nameof(parameters.Type)}::media.video_type IS NULL OR videos.type = @{nameof(parameters.Type)})
               AND (@{nameof(parameters.Viewed)} IS NULL OR (@{nameof(parameters.Viewed)} = TRUE AND video_viewed_by_users.viewed = TRUE) OR (@{nameof(parameters.Viewed)} = FALSE AND video_viewed_by_users.viewed != FALSE))
               AND (@{nameof(parameters.Availability)}::media.external_availability IS NULL OR videos.availability = @{nameof(parameters.Availability)})
             ORDER BY videos.published_at DESC
             LIMIT @{nameof(parameters.Limit)}
             OFFSET @{nameof(parameters.Offset)};
             """,
            parameters,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoEntity>(command);
        return enumerable as List<VideoEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoFileEntity>> GetFilesAsync(
        Guid videoId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             {AccessCte}

             SELECT video_files.id AS Id,
                    video_files.created_at AS CreatedAt,
                    video_files.created_by_user_id AS CreatedByUserId,
                    video_files.modified_at AS ModifiedAt,
                    video_files.modified_by_user_id AS ModifiedByUserId,
                    video_files.owner_id AS OwnerId,
                    video_files.video_id AS VideoId,
                    video_files.storage_path AS StoragePath,
                    video_files.type AS Type,
                    video_files.width AS Width,
                    video_files.height AS Height,
                    video_files.framerate AS Framerate,
                    video_files.downloaded_at AS {nameof(VideoFileEntity.DownloadedAt)},
                    video_files.downloaded_by_user_id AS {nameof(VideoFileEntity.DownloadedByUserId)}
             FROM media.video_files
                INNER JOIN media.videos ON video_files.video_id = videos.id
             WHERE {AccessFilter} AND videos.id = @{nameof(GetVideoParameters.VideoId)}
             ORDER BY video_files.width DESC, video_files.framerate DESC;
             """,
            new GetVideoParameters(videoId, userId, Access.Read),
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoFileEntity>(command);
        return enumerable as List<VideoFileEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoFileEntity>> GetFilesAsync(
        Guid videoId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             {AccessCte}

             SELECT video_files.id AS Id,
                    video_files.created_at AS CreatedAt,
                    video_files.created_by_user_id AS CreatedByUserId,
                    video_files.modified_at AS ModifiedAt,
                    video_files.modified_by_user_id AS ModifiedByUserId,
                    video_files.owner_id AS OwnerId,
                    video_files.video_id AS VideoId,
                    video_files.storage_path AS StoragePath,
                    video_files.type AS Type,
                    video_files.width AS Width,
                    video_files.height AS Height,
                    video_files.framerate AS Framerate,
                    video_files.downloaded_at AS {nameof(VideoFileEntity.DownloadedAt)},
                    video_files.downloaded_by_user_id AS {nameof(VideoFileEntity.DownloadedByUserId)}
             FROM media.video_files
                INNER JOIN media.videos ON video_files.video_id = videos.id
             WHERE {AccessFilter} AND videos.id = @{nameof(GetVideoParameters.VideoId)};
             """,
            new GetVideoParameters(videoId, userId, Access.Read),
            transaction);

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

             SELECT video_files.id AS Id,
                    video_files.created_at AS CreatedAt,
                    video_files.created_by_user_id AS CreatedByUserId,
                    video_files.modified_at AS ModifiedAt,
                    video_files.modified_by_user_id AS ModifiedByUserId,
                    video_files.owner_id AS OwnerId,
                    video_files.video_id AS VideoId,
                    video_files.storage_path AS StoragePath,
                    video_files.type AS Type,
                    video_files.width AS Width,
                    video_files.height AS Height,
                    video_files.framerate AS Framerate,
                    video_files.downloaded_at AS {nameof(VideoFileEntity.DownloadedAt)},
                    video_files.downloaded_by_user_id AS {nameof(VideoFileEntity.DownloadedByUserId)}
             FROM media.video_files
                INNER JOIN media.videos ON video_files.video_id = videos.id
             WHERE {AccessFilter} AND video_files.id = @{nameof(GetSingleParameters.Id)};
             """,
            new GetSingleParameters(id, userId, Access.Read),
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleOrDefaultAsync<VideoFileEntity>(command);
    }

    public async ValueTask<VideoEntity?> FindByExternalId(string externalId, Guid userId, Access access,
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
             SELECT videos.id AS Id,
                    videos.external_id AS ExternalId
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

    public async ValueTask<int> UpdatePlaybackPosition(Guid videoId, Guid userId, double position, NpgsqlTransaction transaction)
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
}
