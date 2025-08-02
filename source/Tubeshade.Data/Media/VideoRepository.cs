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
         INSERT INTO media.videos (created_by_user_id, modified_by_user_id, owner_id, name, description, categories, tags, view_count, like_count, channel_id, storage_path, external_id, external_url, published_at, refreshed_at, availability, duration, ignored_at, ignored_by_user_id) 
         VALUES (@CreatedByUserId, @ModifiedByUserId, @OwnerId, @Name, @Description, @Categories, @Tags, @ViewCount, @LikeCount, @ChannelId, @StoragePath, @ExternalId, @ExternalUrl, @PublishedAt, @RefreshedAt, @Availability, @Duration, @IgnoredAt, @IgnoredByUserId)
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
                videos.ignored_by_user_id AS IgnoredByUserId
         FROM media.videos
         """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        $"""
             name = @{nameof(VideoEntity.Name)},
             channel_id = @{nameof(VideoEntity.ChannelId)},
             storage_path = @{nameof(VideoEntity.StoragePath)},
             external_id = @{nameof(VideoEntity.ExternalId)},
             published_at = @PublishedAt,
             refreshed_at = @RefreshedAt,
             availability = @Availability,
             duration = @Duration,
             ignored_at = @IgnoredAt,
             ignored_by_user_id = @IgnoredByUserId
         """;

    public async ValueTask<List<VideoEntity>> GetDownloadableVideos(
        Guid libraryId,
        Guid userId,
        int limit,
        int offset,
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
               AND EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND downloaded_at IS NULL)
               AND library_channels.library_id = @{nameof(GetFromLibraryParameters.LibraryId)}
             ORDER BY videos.published_at DESC
             LIMIT @Limit
             OFFSET @Offset;
             """,
            new GetFromLibraryParameters(userId, libraryId, Access.Read)
            {
                Limit = limit,
                Offset = offset,
            },
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoEntity>(command);
        return enumerable as List<VideoEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoEntity>> GetDownloadableVideos(
        Guid libraryId,
        Guid userId,
        int limit,
        int offset,
        string query,
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
               AND EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND downloaded_at IS NULL)
               AND library_channels.library_id = @{nameof(GetFromLibraryParameters.LibraryId)}
               AND videos.searchable_index_value @@ websearch_to_tsquery('english', @{nameof(GetFromLibraryParameters.Query)})
             ORDER BY videos.published_at DESC
             LIMIT @Limit
             OFFSET @Offset;
             """,
            new GetFromLibraryParameters(userId, libraryId, Access.Read)
            {
                Limit = limit,
                Offset = offset,
                Query = query,
            },
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoEntity>(command);
        return enumerable as List<VideoEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoEntity>> GetDownloadableVideos(
        Guid libraryId,
        Guid channelId,
        Guid userId,
        int limit,
        int offset,
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
               AND EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND downloaded_at IS NULL)
               AND library_channels.library_id = @{nameof(GetFromLibraryChannelParameters.LibraryId)}
               AND videos.channel_id = @{nameof(GetFromLibraryChannelParameters.ChannelId)}
             ORDER BY videos.published_at DESC
             LIMIT @Limit
             OFFSET @Offset;
             """,
            new GetFromLibraryChannelParameters(userId, libraryId, channelId, Access.Read)
            {
                Limit = limit,
                Offset = offset,
            },
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoEntity>(command);
        return enumerable as List<VideoEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoEntity>> GetForLibrary(
        Guid libraryId,
        Guid userId,
        int limit,
        int offset,
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
               AND library_channels.library_id = @LibraryId
               AND videos.ignored_at IS NULL
               AND EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND video_files.downloaded_at IS NOT NULL)
             ORDER BY videos.published_at DESC
             LIMIT @Limit
             OFFSET @Offset;
             """,
            new GetFromLibraryParameters(userId, libraryId, Access.Read)
            {
                Limit = limit,
                Offset = offset,
            },
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoEntity>(command);
        return enumerable as List<VideoEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoEntity>> GetForLibrary(
        Guid libraryId,
        Guid userId,
        int limit,
        int offset,
        string query,
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
               AND library_channels.library_id = @LibraryId
               AND videos.ignored_at IS NULL
               AND EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND video_files.downloaded_at IS NOT NULL)
               AND videos.searchable_index_value @@ websearch_to_tsquery('english', @{nameof(GetFromLibraryParameters.Query)})
             ORDER BY videos.published_at DESC
             LIMIT @Limit
             OFFSET @Offset;
             """,
            new GetFromLibraryParameters(userId, libraryId, Access.Read)
            {
                Limit = limit,
                Offset = offset,
                Query = query,
            },
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoEntity>(command);
        return enumerable as List<VideoEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoEntity>> GetForChannel(
        Guid channelId,
        Guid userId,
        int limit,
        int offset,
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
             WHERE {AccessFilter}
               AND channels.id = @ChannelId
               AND videos.ignored_at IS NULL
               AND EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND video_files.downloaded_at IS NOT NULL)
             ORDER BY videos.published_at DESC
             LIMIT @Limit
             OFFSET @Offset;
             """,
            new GetFromChannelParameters(userId, channelId, Access.Read)
            {
                Limit = limit,
                Offset = offset,
            },
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoEntity>(command);
        return enumerable as List<VideoEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoEntity>> GetForChannel(
        Guid channelId,
        Guid userId,
        int limit,
        int offset,
        string query,
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
             WHERE {AccessFilter}
               AND channels.id = @ChannelId
               AND videos.ignored_at IS NULL
               AND EXISTS(SELECT 1 FROM media.video_files WHERE video_files.video_id = videos.id AND video_files.downloaded_at IS NOT NULL)
               AND videos.searchable_index_value @@ websearch_to_tsquery('english', @{nameof(GetFromChannelParameters.Query)})
             ORDER BY videos.published_at DESC
             LIMIT @Limit
             OFFSET @Offset;
             """,
            new GetFromChannelParameters(userId, channelId, Access.Read)
            {
                Limit = limit,
                Offset = offset,
                Query = query,
            },
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

    public async ValueTask<VideoEntity?> FindByExternalId(string externalId, Guid userId, Access access, NpgsqlTransaction transaction)
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

    public async ValueTask<VideoEntity?> FindByExternalUrl(string externalUrl, Guid userId, Access access, NpgsqlTransaction transaction)
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
        var enumerable = await Connection.QueryAsync<EntityId>( // lang=sql
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
}
