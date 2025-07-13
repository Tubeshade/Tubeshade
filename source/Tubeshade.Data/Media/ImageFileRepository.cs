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
         INSERT INTO media.image_files (created_by_user_id, storage_path, type, width, height) 
         VALUES (@CreatedByUserId, @StoragePath, @Type, @Width, @Height)
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
                height AS {nameof(ImageFileEntity.Height)}
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
}
