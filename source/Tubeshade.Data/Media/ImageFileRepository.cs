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

/// <remarks>
/// Images have no owner of their own: access is inherited from the primary library of the video, channel or playlist
/// that the image belongs to.
/// </remarks>
public sealed class ImageFileRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<ImageFileEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.image_files";

    /// <inheritdoc />
    /// <remarks>An image is only reachable once it is linked to a video, channel or playlist.</remarks>
    protected override string InsertSql =>
        """
        INSERT INTO media.image_files (created_by_user_id, modified_by_user_id, storage_path, type, width, height, hash_algorithm, hash, storage_size)
        VALUES (@CreatedByUserId, @ModifiedByUserId, @StoragePath, @Type, @Width, @Height, @HashAlgorithm, @Hash, @StorageSize)
        RETURNING id;
        """;

    /// <inheritdoc />
    protected override string SelectSql =>
        """
        SELECT id,
               created_at,
               created_by_user_id,
               modified_at,
               modified_by_user_id,
               storage_path,
               type,
               width,
               height,
               hash_algorithm,
               hash,
               storage_size
        FROM media.image_files
        """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        $"""
           storage_path = @{nameof(ImageFileEntity.StoragePath)},
           type = @{nameof(ImageFileEntity.Type)},
           width = @{nameof(ImageFileEntity.Width)},
           height = @{nameof(ImageFileEntity.Height)},
           hash_algorithm = @{nameof(ImageFileEntity.HashAlgorithm)},
           hash = @{nameof(ImageFileEntity.Hash)},
           storage_size = @{nameof(ImageFileEntity.StorageSize)}
         """;

    /// <inheritdoc />
    protected override string AccessFilter => $"(image_files.id IN ({ImagesInLibraries(AccessibleLibraries)}))";

    /// <inheritdoc />
    protected override string UpdateAccessFilter =>
        $"(image_files.id IN ({ImagesInLibraries($"SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN ({UpdatableOwners})")}))";

    /// <summary>Links the image to the video, if the user holds Update on the video.</summary>
    public async ValueTask<int> LinkToVideoAsync(Guid id, Guid videoId, Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             INSERT INTO media.video_images (video_id, image_id)
             SELECT videos.id, @{nameof(id)}
             FROM media.videos
             WHERE videos.id = @{nameof(videoId)}
               AND videos.owner_id IN ({Access.AccessibleOwners});
             """,
            Access.Parameters(new UserPrincipal(userId), Resources.Video, Permissions.Update, new { id, videoId }),
            transaction);

        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask<List<ImageFileEntity>> GetForVideo(
        Guid videoId,
        Guid userId,
        Permissions permissions,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new GetVideoParameters(videoId, userId, permissions | Permissions.Read);

        var command = new CommandDefinition(
            // lang=sql
            $"""
             {SelectSql}
                 INNER JOIN media.video_images ON image_files.id = video_images.image_id
             WHERE video_images.video_id IN (SELECT videos.id
                                             FROM media.videos
                                                 INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
                                             WHERE videos.id = @{nameof(parameters.VideoId)}
                                               AND library_channels.library_id IN ({AccessibleLibraries}))
             ORDER BY image_files.width, image_files.height;
             """,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<ImageFileEntity>(command);
        return enumerable as List<ImageFileEntity> ?? enumerable.ToList();
    }

    /// <summary>Links the image to the channel, if the user holds Update on the channel.</summary>
    public async ValueTask<int> LinkToChannelAsync(Guid id, Guid channelId, Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             INSERT INTO media.channel_images (channel_id, image_id)
             SELECT channels.id, @{nameof(id)}
             FROM media.channels
             WHERE channels.id = @{nameof(channelId)}
               AND channels.owner_id IN ({Access.AccessibleOwners});
             """,
            Access.Parameters(new UserPrincipal(userId), Resources.Channel, Permissions.Update, new { id, channelId }),
            transaction);

        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask<List<ImageFileEntity>> GetForChannel(
        Guid channelId,
        Guid userId,
        Permissions permissions,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new GetVideoParameters(channelId, userId, permissions | Permissions.Read);

        var command = new CommandDefinition(
            // lang=sql
            $"""
             {SelectSql}
                 INNER JOIN media.channel_images ON image_files.id = channel_images.image_id
             WHERE channel_images.channel_id IN (SELECT library_channels.channel_id
                                                 FROM media.library_channels
                                                 WHERE library_channels.channel_id = @{nameof(parameters.VideoId)}
                                                   AND library_channels."primary"
                                                   AND library_channels.library_id IN ({AccessibleLibraries}))
             ORDER BY image_files.width, image_files.height;
             """,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<ImageFileEntity>(command);
        return enumerable as List<ImageFileEntity> ?? enumerable.ToList();
    }

    /// <summary>Links the image to the playlist, if the user holds Update on the playlist's library.</summary>
    public async ValueTask<int> LinkToPlaylistAsync(Guid id, Guid playlistId, Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             INSERT INTO media.playlist_images (playlist_id, image_id)
             SELECT playlists.id, @{nameof(id)}
             FROM media.playlists
             WHERE playlists.id = @{nameof(playlistId)}
               AND playlists.library_id IN ({AccessibleLibraries});
             """,
            Access.Parameters(new UserPrincipal(userId), Resources.Playlist, Permissions.Update, new { id, playlistId }),
            transaction);

        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask<List<ImageFileEntity>> GetForPlaylist(
        Guid playlistId,
        Guid userId,
        Permissions permissions,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new GetVideoParameters(playlistId, userId, permissions | Permissions.Read);

        var command = new CommandDefinition(
            // lang=sql
            $"""
             {SelectSql}
                 INNER JOIN media.playlist_images ON image_files.id = playlist_images.image_id
             WHERE playlist_images.playlist_id IN (SELECT playlists.id
                                                   FROM media.playlists
                                                   WHERE playlists.id = @{nameof(parameters.VideoId)}
                                                     AND playlists.library_id IN ({AccessibleLibraries}))
             ORDER BY image_files.width, image_files.height;
             """,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<ImageFileEntity>(command);
        return enumerable as List<ImageFileEntity> ?? enumerable.ToList();
    }

    /// <summary>Gets the storage path of the video, channel or playlist that the image belongs to, if the user can read it.</summary>
    public async ValueTask<string?> GetPath(
        Guid id,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             SELECT channels.storage_path
             FROM media.channels
                INNER JOIN media.channel_images ON channels.id = channel_images.channel_id
             WHERE channel_images.image_id = @{nameof(GetSingleParameters.Id)}
               AND channels.owner_id IN ({Access.AccessibleOwners})

             UNION ALL

             SELECT videos.storage_path
             FROM media.videos
                INNER JOIN media.video_images ON videos.id = video_images.video_id
             WHERE video_images.image_id = @{nameof(GetSingleParameters.Id)}
               AND videos.owner_id IN ({Access.AccessibleOwners})

             UNION ALL

             SELECT playlists.storage_path
             FROM media.playlists
                INNER JOIN media.playlist_images ON playlists.id = playlist_images.playlist_id
             WHERE playlist_images.image_id = @{nameof(GetSingleParameters.Id)}
               AND playlists.library_id IN ({AccessibleLibraries});
             """,
            new GetSingleParameters(id, userId, Permissions.Read),
            transaction,
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleOrDefaultAsync<string>(command);
    }

    private static string ImagesInLibraries(string libraryIds) =>
        // lang=sql
        $"""
         SELECT video_images.image_id
         FROM media.videos
             INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
             INNER JOIN media.video_images ON videos.id = video_images.video_id
         WHERE library_channels.library_id IN ({libraryIds})

         UNION ALL

         SELECT channel_images.image_id
         FROM media.library_channels
             INNER JOIN media.channel_images ON library_channels.channel_id = channel_images.channel_id
         WHERE library_channels."primary"
           AND library_channels.library_id IN ({libraryIds})

         UNION ALL

         SELECT playlist_images.image_id
         FROM media.playlists
             INNER JOIN media.playlist_images ON playlists.id = playlist_images.playlist_id
         WHERE playlists.library_id IN ({libraryIds})
         """;
}
