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

public sealed class ImageFileRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<ImageFileEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.image_files";

    /// <inheritdoc />
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
             (SELECT video_images.image_id
              FROM media.videos
                  INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
                  INNER JOIN accessible_libraries ON library_channels.library_id = accessible_libraries.id
                  INNER JOIN media.video_images ON videos.id = video_images.video_id)
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
             (SELECT video_images.image_id
              FROM media.videos
                  INNER JOIN media.library_channels ON library_channels.channel_id = videos.channel_id AND library_channels."primary"
                  INNER JOIN accessible_libraries ON library_channels.library_id = accessible_libraries.id
                  INNER JOIN media.video_images ON videos.id = video_images.video_id)
         """;

    /// <inheritdoc />
    protected override string AccessFilter => "(image_files.id IN (SELECT image_id FROM accessible))";

    public async ValueTask<int> LinkToVideoAsync(Guid id, Guid videoId, Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            """
            INSERT INTO media.video_images (video_id, image_id)
            VALUES (@VideoId, @Id);
            """,
            new GetSingleVideoParameters(id, videoId, userId, Access.Read),
            transaction);

        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask<ImageFileEntity?> FindVideoThumbnail(
        Guid videoId,
        Guid userId,
        Access access,
        NpgsqlTransaction transaction)
    {
        var parameters = new GetVideoParameters(videoId, userId, access);

        var command = new CommandDefinition(
            // lang=sql
            $"""
             {AccessCte}

             {SelectSql}
                 INNER JOIN media.video_images ON image_files.id = video_images.image_id
                 INNER JOIN accessible ON image_files.id = accessible.image_id
             WHERE {AccessFilter} AND
                   video_images.video_id = @{nameof(parameters.VideoId)} AND
                   image_files.type = 'thumbnail';
             """,
            parameters,
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<ImageFileEntity>(command);
    }

    public async ValueTask<List<ImageFileEntity>> GetForVideo(
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
                 INNER JOIN media.video_images ON image_files.id = video_images.image_id
                 INNER JOIN accessible ON image_files.id = accessible.image_id
             WHERE {AccessFilter} AND
                   video_images.video_id = @{nameof(parameters.VideoId)};
             """,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<ImageFileEntity>(command);
        return enumerable as List<ImageFileEntity> ?? enumerable.ToList();
    }
}
