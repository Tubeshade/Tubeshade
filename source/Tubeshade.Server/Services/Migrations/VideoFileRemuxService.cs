using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Services.Ffmpeg;

namespace Tubeshade.Server.Services.Migrations;

public sealed class VideoFileRemuxService
{
    private readonly ILogger<VideoFileRemuxService> _logger;
    private readonly NpgsqlConnection _connection;
    private readonly VideoRepository _videoRepository;
    private readonly VideoFileRepository _videoFileRepository;
    private readonly FfmpegService _ffmpegService;
    private readonly IClock _clock;

    public VideoFileRemuxService(
        ILogger<VideoFileRemuxService> logger,
        NpgsqlConnection connection,
        VideoRepository videoRepository,
        VideoFileRepository videoFileRepository,
        FfmpegService ffmpegService,
        IClock clock)
    {
        _logger = logger;
        _connection = connection;
        _videoRepository = videoRepository;
        _videoFileRepository = videoFileRepository;
        _ffmpegService = ffmpegService;
        _clock = clock;
    }

    public async Task RemuxVideoFiles(
        Guid libraryId,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        CancellationToken cancellationToken = default)
    {
        var hashAlgorithm = HashAlgorithm.Default;

        var videos = await _videoRepository.GetFiltered(
            new VideoParameters
            {
                UserId = userId,
                Limit = int.MaxValue,
                Offset = 0,
                SortBy = SortVideoBy.PublishedAt,
                SortDirection = SortDirection.Descending,
                LibraryId = libraryId,
            },
            cancellationToken);

        var totalCount = videos.Count;
        await taskRepository.InitializeTaskProgress(taskRunId, totalCount);
        var startTime = _clock.GetCurrentInstant();

        foreach (var (index, video) in videos.Index())
        {
            List<VideoFileEntity> files;
            await using (var transaction = await _connection.OpenAndBeginTransaction(cancellationToken))
            {
                files = await _videoRepository.GetFilesAsync(video.Id, userId, transaction, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            foreach (var videoFile in files)
            {
                if (videoFile is { DownloadedAt: null } || videoFile.Type != VideoContainerType.Mp4)
                {
                    continue;
                }

                var file = new FileInfo(Path.Combine(video.StoragePath, videoFile.StoragePath));
                if (!file.Exists)
                {
                    _logger.VideoFileDoesNotExist(videoFile.Id, video.Id, file.FullName);
                    continue;
                }

                var remuxedPath = Path.Combine(
                    file.DirectoryName!,
                    $"{Path.GetFileNameWithoutExtension(file.Name)}_remux{file.Extension}");

                try
                {
                    if (await Mp4Layout.IsStreamable(file.FullName, cancellationToken))
                    {
                        continue;
                    }

                    _logger.RemuxingVideoFile(videoFile.Id, file.FullName);
                    await _ffmpegService.Copy(file.FullName, remuxedPath, cancellationToken);
                    var remuxedFile = new FileInfo(remuxedPath);
                    var hash = await hashAlgorithm.ComputeHashAsync(remuxedFile, cancellationToken);

                    await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

                    if (await _videoFileRepository.DeleteAsync(videoFile.Id, userId, transaction) is 0)
                    {
                        throw new InvalidOperationException($"Failed to delete file {videoFile.Id} for video {video.Id}");
                    }

                    var replacement = videoFile with
                    {
                        CreatedByUserId = userId,
                        ModifiedByUserId = userId,
                        Hash = hash,
                        HashAlgorithm = hashAlgorithm,
                        StorageSize = remuxedFile.Length,
                    };

                    if (await _videoFileRepository.AddAsync(replacement, transaction) is not { } replacementId)
                    {
                        throw new InvalidOperationException($"Failed to add remuxed file for video {video.Id}");
                    }

                    await transaction.CommitAsync(cancellationToken);

                    // If moving fails, the old file is still in place and will be remuxed again during the next run
                    File.Move(remuxedPath, file.FullName, true);
                    _logger.RemuxedVideoFile(videoFile.Id, replacementId);
                }
                catch (OperationCanceledException)
                {
                    File.Delete(remuxedPath);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.RemuxingVideoFileFailed(exception, videoFile.Id, file.FullName);
                    File.Delete(remuxedPath);
                }
            }

            var currentIndex = index + 1;
            var (rate, remaining) = _clock.GetRemainingEstimate(startTime, totalCount, currentIndex);
            await taskRepository.UpdateProgress(taskRunId, currentIndex, rate, remaining);
        }
    }
}
