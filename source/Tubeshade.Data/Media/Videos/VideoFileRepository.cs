using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Videos;

public sealed class VideoFileRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<VideoFileEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.video_files";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO media.video_files (created_by_user_id, modified_by_user_id, owner_id, video_id, storage_path, type, width, height, framerate, downloaded_at, downloaded_by_user_id, hash_algorithm, hash, storage_size)
         SELECT @CreatedByUserId, @ModifiedByUserId, @OwnerId, @VideoId, @StoragePath, @Type, @Width, @Height, @Framerate,@DownloadedAt, @DownloadedByUserId, @HashAlgorithm, @Hash, @StorageSize
         WHERE {CanCreateIn(nameof(VideoFileEntity.OwnerId))}
         RETURNING id;
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        $"""
         SELECT id,
                created_at,
                created_by_user_id,
                modified_at,
                modified_by_user_id,
                owner_id,
                video_id,
                storage_path,
                type,
                width,
                height,
                framerate,
                downloaded_at,
                downloaded_by_user_id,
                hash_algorithm,
                hash,
                storage_size
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
             hash_algorithm = @{nameof(VideoFileEntity.HashAlgorithm)},
             hash = @{nameof(VideoFileEntity.Hash)},
             storage_size = @{nameof(VideoFileEntity.StorageSize)}
         """;

    /// <summary>Records where the file is being downloaded to, if the user holds Update on the file.</summary>
    public async ValueTask<bool> CreateTemporaryFile(
        Guid id,
        Guid userId,
        Guid taskRunId,
        string path,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             INSERT INTO media.video_files_downloading (file_id, task_run_id, path)
             SELECT video_files.id, @{nameof(taskRunId)}, @{nameof(path)}
             FROM media.video_files
             WHERE video_files.id = @{nameof(id)}
               AND video_files.owner_id IN ({Access.AccessibleOwners})
             ON CONFLICT (file_id) DO
                 UPDATE SET task_run_id = @{nameof(taskRunId)},
                            path = @{nameof(path)};
             """,
            Access.Parameters(new UserPrincipal(userId), Resources.File, Permissions.Update, new { id, taskRunId, path }),
            cancellationToken: cancellationToken);

        return await Connection.ExecuteAsync(command) is 1;
    }
}
