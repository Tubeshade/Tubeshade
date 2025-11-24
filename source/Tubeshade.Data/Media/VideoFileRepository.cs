using Npgsql;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed class VideoFileRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<VideoFileEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.video_files";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO media.video_files (created_by_user_id, modified_by_user_id, owner_id, video_id, storage_path, type, width, height, framerate, downloaded_at, downloaded_by_user_id, hash, hash_algorithm) 
         VALUES (@CreatedByUserId, @ModifiedByUserId, @OwnerId, @VideoId, @StoragePath, @Type, @Width, @Height, @Framerate,@DownloadedAt, @DownloadedByUserId, @Hash, @HashAlgorithm)
         RETURNING id;
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        $"""
         SELECT id AS {nameof(VideoFileEntity.Id)},
                created_at AS {nameof(VideoFileEntity.CreatedAt)},
                created_by_user_id AS {nameof(VideoFileEntity.CreatedByUserId)},
                modified_at AS {nameof(VideoFileEntity.ModifiedAt)},
                modified_by_user_id AS {nameof(VideoFileEntity.ModifiedByUserId)},
                owner_id AS {nameof(VideoFileEntity.OwnerId)},
                video_id AS {nameof(VideoFileEntity.VideoId)},
                storage_path AS {nameof(VideoFileEntity.StoragePath)},
                type AS {nameof(VideoFileEntity.Type)},
                width AS {nameof(VideoFileEntity.Width)},
                height AS {nameof(VideoFileEntity.Height)},
                framerate AS {nameof(VideoFileEntity.Framerate)},
                downloaded_at AS {nameof(VideoFileEntity.DownloadedAt)},
                downloaded_by_user_id AS {nameof(VideoFileEntity.DownloadedByUserId)},
                hash AS {nameof(VideoFileEntity.Hash)},
                hash_algorithm AS {nameof(VideoFileEntity.HashAlgorithm)}
         FROM media.video_files
         """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        $"""
             video_id = @{nameof(VideoFileEntity.VideoId)},
             storage_path = @{nameof(VideoFileEntity.StoragePath)},
             type = @{nameof(VideoFileEntity.Type)},
             width = @{nameof(VideoFileEntity.Width)},
             height = @{nameof(VideoFileEntity.Height)},
             framerate = @{nameof(VideoFileEntity.Framerate)},
             downloaded_at = @{nameof(VideoFileEntity.DownloadedAt)},
             downloaded_by_user_id = @{nameof(VideoFileEntity.DownloadedByUserId)},
             hash = @{nameof(VideoFileEntity.Hash)},
             hash_algorithm = @{nameof(VideoFileEntity.HashAlgorithm)}
         """;
}
