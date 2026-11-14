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

public sealed class TrackFileRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<TrackFileEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.track_files";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO media.track_files (created_by_user_id, modified_by_user_id, video_id, storage_path, type, language, hash, hash_algorithm, storage_size)
         SELECT @CreatedByUserId, @ModifiedByUserId, @VideoId, @StoragePath, @Type, @Language, @Hash, @HashAlgorithm, @StorageSize
         WHERE @{nameof(TrackFileEntity.VideoId)} IN (SELECT videos.id
                                                     FROM media.videos
                                                     WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@{nameof(IEntity.CreatedByUserId)}, {(int)Permissions.Update})))
         RETURNING id;
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        """
        SELECT track_files.id,
               track_files.created_at,
               track_files.created_by_user_id,
               track_files.modified_at,
               track_files.modified_by_user_id,
               track_files.video_id,
               track_files.storage_path,
               track_files.type,
               track_files.language,
               track_files.hash,
               track_files.hash_algorithm,
               track_files.storage_size
        FROM media.track_files
        """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        $"""
           video_id = @{nameof(TrackFileEntity.VideoId)},
           storage_path = @{nameof(TrackFileEntity.StoragePath)},
           type = @{nameof(TrackFileEntity.Type)},
           language = @{nameof(TrackFileEntity.Language)},
           hash_algorithm = @{nameof(TrackFileEntity.HashAlgorithm)},
           hash = @{nameof(TrackFileEntity.Hash)},
           storage_size = @{nameof(TrackFileEntity.StorageSize)}
         """;

    /// <inheritdoc />
    /// <remarks>Track files have no owner of their own - access is inherited from the video's primary library.</remarks>
    protected override string AccessFilter => $"(track_files.video_id IN ({VideosInLibraries(AccessibleLibraries)}))";

    /// <inheritdoc />
    protected override string UpdateAccessFilter =>
        $"(track_files.video_id IN ({VideosInLibraries($"SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN ({UpdatableOwners})")}))";

    public async ValueTask<List<TrackFileEntity>> GetForVideo(
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
             WHERE track_files.video_id IN (SELECT videos.id
                                            FROM media.videos
                                                INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
                                            WHERE videos.id = @{nameof(parameters.VideoId)}
                                              AND library_channels.library_id IN ({AccessibleLibraries}));
             """,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<TrackFileEntity>(command);
        return enumerable as List<TrackFileEntity> ?? enumerable.ToList();
    }

    private static string VideosInLibraries(string libraryIds) =>
        // lang=sql
        $"""
         SELECT videos.id
         FROM media.videos
             INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
         WHERE library_channels.library_id IN ({libraryIds})
         """;
}
