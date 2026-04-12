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
        """
        INSERT INTO media.track_files (created_by_user_id, modified_by_user_id, video_id, storage_path, type, language, hash, hash_algorithm, storage_size)
        VALUES (@CreatedByUserId, @ModifiedByUserId, @VideoId, @StoragePath, @Type, @Language, @Hash, @HashAlgorithm, @StorageSize)
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
    protected override string AccessCte =>
        // lang=sql
        $"""
         WITH accessible_libraries AS
         (SELECT libraries.id
          FROM media.libraries
              INNER JOIN identity.owners ON owners.id = libraries.owner_id
              INNER JOIN identity.ownerships ON
                  ownerships.owner_id = owners.id AND
                  ownerships.user_id = @{nameof(GetVideoParameters.UserId)} AND
                  (ownerships.access = @{nameof(GetVideoParameters.Access)} OR ownerships.access = 'owner')),
         accessible AS
            (SELECT track_files.id
             FROM media.videos
                 INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
                 INNER JOIN accessible_libraries ON library_channels.library_id = accessible_libraries.id
                 INNER JOIN media.track_files ON videos.id = track_files.video_id)
         """;

    /// <inheritdoc />
    protected override string UpdateAccessCte =>
        // lang=sql
        $"""
         WITH accessible_libraries AS
         (SELECT libraries.id
          FROM media.libraries
              INNER JOIN identity.owners ON owners.id = libraries.owner_id
              INNER JOIN identity.ownerships ON
                  ownerships.owner_id = owners.id AND
                  ownerships.user_id = @{nameof(IModifiableEntity.ModifiedByUserId)} AND
                  (ownerships.access = @{nameof(IModifiableEntity.AccessFoo)} OR ownerships.access = 'owner')),
         accessible AS
             (SELECT track_files.id
              FROM media.videos
                  INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
                  INNER JOIN accessible_libraries ON library_channels.library_id = accessible_libraries.id
                  INNER JOIN media.track_files ON videos.id = track_files.video_id)
         """;

    /// <inheritdoc />
    protected override string AccessFilter => "(track_files.id IN (SELECT id FROM accessible))";

    public async ValueTask<List<TrackFileEntity>> GetForVideo(
        Guid videoId,
        Guid userId,
        Access access,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new GetVideoParameters(videoId, userId, access);

        var command = new CommandDefinition(
            // lang=sql
            $"""
             {AccessCte}

             {SelectSql}
                 INNER JOIN accessible ON track_files.id = accessible.id
             WHERE {AccessFilter} AND
                   track_files.video_id = @{nameof(parameters.VideoId)};
             """,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<TrackFileEntity>(command);
        return enumerable as List<TrackFileEntity> ?? enumerable.ToList();
    }
}
