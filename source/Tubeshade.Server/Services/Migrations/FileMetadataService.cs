using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Services.Migrations;

public sealed class FileMetadataService
{
    private readonly ILogger<FileMetadataService> _logger;
    private readonly NpgsqlConnection _connection;
    private readonly VideoRepository _videoRepository;
    private readonly VideoFileRepository _videoFileRepository;
    private readonly ImageFileRepository _imageFileRepository;
    private readonly IClock _clock;

    public FileMetadataService(
        ILogger<FileMetadataService> logger,
        NpgsqlConnection connection,
        VideoRepository videoRepository,
        VideoFileRepository videoFileRepository,
        ImageFileRepository imageFileRepository,
        IClock clock)
    {
        _logger = logger;
        _connection = connection;
        _videoRepository = videoRepository;
        _videoFileRepository = videoFileRepository;
        _imageFileRepository = imageFileRepository;
        _clock = clock;
    }

    public async Task AddMissingMetadata(
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
            await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
            var files = await _videoRepository.GetFilesAsync(video.Id, userId, transaction, cancellationToken);

            foreach (var videoFile in files)
            {
                if (videoFile is { HashAlgorithm.Name: not HashAlgorithm.Names.Placeholder } or { DownloadedAt: null })
                {
                    continue;
                }

                var file = new FileInfo(Path.Combine(video.StoragePath, videoFile.StoragePath));
                if (!file.Exists)
                {
                    _logger.VideoFileDoesNotExist(videoFile.Id, video.Id, file.FullName);
                    continue;
                }

                if (videoFile.HashAlgorithm is null)
                {
                    _logger.AddingVideoMetadata(hashAlgorithm.Name, videoFile.Id, file.FullName);
                }
                else
                {
                    _logger.ReplacingVideoPlaceholderHash(hashAlgorithm.Name, videoFile.Id, file.FullName);
                }

                var hashData = await hashAlgorithm.ComputeHashAsync(file, cancellationToken);

                videoFile.ModifiedAt = _clock.GetCurrentInstant();
                videoFile.ModifiedByUserId = userId;
                videoFile.Hash = hashData;
                videoFile.HashAlgorithm = hashAlgorithm;
                videoFile.StorageSize = file.Length;

                if (await _videoFileRepository.UpdateAsync(videoFile, transaction) is not 1)
                {
                    throw new InvalidOperationException($"Failed to update file {videoFile.Id} for video {video.Id}");
                }
            }

            var images = await _imageFileRepository.GetForVideo(video.Id, userId, Access.Modify, transaction, cancellationToken);
            foreach (var imageFile in images)
            {
                if (imageFile is { HashAlgorithm.Name: not HashAlgorithm.Names.Placeholder })
                {
                    continue;
                }

                var file = new FileInfo(Path.Combine(video.StoragePath, imageFile.StoragePath));
                if (!file.Exists)
                {
                    _logger.ImageFileDoesNotExist(imageFile.Id, video.Id, file.FullName);
                    continue;
                }

                _logger.ReplacingImagePlaceholderHash(hashAlgorithm.Name, imageFile.Id, file.FullName);
                var hashData = await hashAlgorithm.ComputeHashAsync(file, cancellationToken);

                imageFile.ModifiedAt = _clock.GetCurrentInstant();
                imageFile.ModifiedByUserId = userId;
                imageFile.Hash = hashData;
                imageFile.HashAlgorithm = hashAlgorithm;
                imageFile.StorageSize = file.Length;

                if (await _imageFileRepository.UpdateAsync(imageFile, transaction) is not 1)
                {
                    throw new InvalidOperationException($"Failed to update file {imageFile.Id}");
                }
            }

            await transaction.CommitAsync(cancellationToken);

            var currentIndex = index + 1;
            var (rate, remaining) = _clock.GetRemainingEstimate(startTime, totalCount, currentIndex);
            await taskRepository.UpdateProgress(taskRunId, currentIndex, rate, remaining);
        }
    }
}
