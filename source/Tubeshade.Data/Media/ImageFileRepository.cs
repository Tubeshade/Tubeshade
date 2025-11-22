using System;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media;

public sealed class ImageFileRepository(NpgsqlConnection connection) : RepositoryBase<ImageFileEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.image_files";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO media.image_files (created_by_user_id, storage_path, type, width, height, hash, hash_algorithm) 
         VALUES (@CreatedByUserId, @StoragePath, @Type, @Width, @Height, @Hash, @HashAlgorithm)
         RETURNING id;
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        $"""
         SELECT id AS {nameof(ImageFileEntity.Id)},
                created_at AS {nameof(ImageFileEntity.CreatedAt)},
                created_by_user_id AS {nameof(ImageFileEntity.CreatedByUserId)},
                storage_path AS {nameof(ImageFileEntity.StoragePath)},
                type AS {nameof(ImageFileEntity.Type)},
                width AS {nameof(ImageFileEntity.Width)},
                height AS {nameof(ImageFileEntity.Height)},
                hash AS {nameof(VideoFileEntity.Hash)},
                hash_algorithm AS {nameof(VideoFileEntity.HashAlgorithm)}
         FROM media.image_files
         """;

    public async ValueTask<int> LinkToVideoAsync(Guid id, Guid videoId, Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             INSERT INTO media.video_images (video_id, image_id)
             VALUES (@VideoId, @Id);
             """,
            new GetSingleVideoParameters(id, videoId, userId, Access.Read),
            transaction);

        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask UpdateHashAsync(
        Guid id,
        byte[] hash,
        HashAlgorithm hashAlgorithm,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             UPDATE media.image_files
             SET hash = @{nameof(hash)},
                 hash_algorithm = @{nameof(hashAlgorithm)}
             WHERE id = @{nameof(id)}
             """,
            new { id, hash, hashAlgorithm },
            transaction);

        await Connection.ExecuteAsync(command);
    }

    public async ValueTask<string?> FindBasePathUnsafe(Guid id, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             SELECT videos.storage_path
             FROM media.image_files
                INNER JOIN media.video_images ON image_files.id = video_images.image_id
                INNER JOIN media.videos ON video_images.video_id = videos.id
             WHERE image_files.id = @{nameof(id)};
             """,
            new { id },
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<string>(command);
    }
}
