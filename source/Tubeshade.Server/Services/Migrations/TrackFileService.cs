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
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Pages.Videos;

namespace Tubeshade.Server.Services.Migrations;

public sealed class TrackFileService
{
    private readonly ILogger<TrackFileService> _logger;
    private readonly NpgsqlConnection _connection;
    private readonly VideoRepository _videoRepository;
    private readonly TrackFileRepository _trackFileRepository;
    private readonly IClock _clock;

    public TrackFileService(
        ILogger<TrackFileService> logger,
        NpgsqlConnection connection,
        VideoRepository videoRepository,
        TrackFileRepository trackFileRepository,
        IClock clock)
    {
        _logger = logger;
        _connection = connection;
        _videoRepository = videoRepository;
        _trackFileRepository = trackFileRepository;
        _clock = clock;
    }

    public async Task RefreshTrackFiles(
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
            var currentIndex = index + 1;
            var videoDirectory = new DirectoryInfo(video.StoragePath);
            if (!videoDirectory.Exists)
            {
                var (skippedRate, skippedRemaining) = _clock.GetRemainingEstimate(startTime, totalCount, currentIndex);
                await taskRepository.UpdateProgress(taskRunId, currentIndex, skippedRate, skippedRemaining);
                continue;
            }

            await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
            var tracks = await _trackFileRepository.GetForVideo(video.Id, userId, Access.Read, transaction, cancellationToken);

            var chaptersFile = new FileInfo(video.GetChaptersFilePath());
            if (chaptersFile.Exists)
            {
                await CreateOrUpdateChapters(video, chaptersFile, tracks, hashAlgorithm, userId, transaction, cancellationToken);
            }
            else
            {
                _logger.ChaptersFileDoesNotExist(video.Id, chaptersFile.FullName);
            }

            await CreateOrUpdateSubtitles(video, videoDirectory, tracks, hashAlgorithm, userId, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var (rate, remaining) = _clock.GetRemainingEstimate(startTime, totalCount, currentIndex);
            await taskRepository.UpdateProgress(taskRunId, currentIndex, rate, remaining);
        }
    }

    public async Task CreateOrUpdateSubtitles(
        VideoEntity video,
        DirectoryInfo videoDirectory,
        List<TrackFileEntity> tracks,
        HashAlgorithm hashAlgorithm,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var subtitlesFiles = videoDirectory.EnumerateFiles("subtitles.*.vtt");
        foreach (var subtitlesFile in subtitlesFiles)
        {
            var parts = Path.GetFileNameWithoutExtension(subtitlesFile.FullName).Split('.');
            if (parts is not ["subtitles", var language])
            {
                throw new InvalidOperationException($"Subtitle file with unexpected name: {subtitlesFile.FullName}");
            }

            if (!tracks.TryGetSubtitles(language, out var existing))
            {
                var hash = await hashAlgorithm.ComputeHashAsync(subtitlesFile, cancellationToken);

                var trackFile = new TrackFileEntity
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    VideoId = video.Id,
                    StoragePath = subtitlesFile.Name,
                    Type = TrackType.Subtitles,
                    Language = language.ToUpperInvariant(),
                    Hash = hash,
                    HashAlgorithm = hashAlgorithm,
                    StorageSize = subtitlesFile.Length,
                };

                if (await _trackFileRepository.AddAsync(trackFile, transaction) is null)
                {
                    throw new InvalidOperationException($"Failed to add track file to video {video.Id}");
                }
            }
            else if (existing.HashAlgorithm.Name is HashAlgorithm.Names.Placeholder)
            {
                var hash = await hashAlgorithm.ComputeHashAsync(subtitlesFile, cancellationToken);

                existing.ModifiedAt = _clock.GetCurrentInstant();
                existing.ModifiedByUserId = userId;
                existing.Hash = hash;
                existing.HashAlgorithm = hashAlgorithm;
                existing.StorageSize = subtitlesFile.Length;

                if (await _trackFileRepository.UpdateAsync(existing, transaction) is not 1)
                {
                    throw new InvalidOperationException($"Failed to update track file for video {video.Id}");
                }

                tracks.Remove(existing);
            }
        }
    }

    public async Task CreateOrUpdateChapters(
        VideoEntity video,
        FileInfo chaptersFile,
        List<TrackFileEntity> tracks,
        HashAlgorithm hashAlgorithm,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (!tracks.TryGetChapters(out var existing))
        {
            var hash = await hashAlgorithm.ComputeHashAsync(chaptersFile, cancellationToken);

            var trackFile = new TrackFileEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                VideoId = video.Id,
                StoragePath = chaptersFile.Name,
                Type = TrackType.Chapters,
                Hash = hash,
                HashAlgorithm = hashAlgorithm,
                StorageSize = chaptersFile.Length,
            };

            if (await _trackFileRepository.AddAsync(trackFile, transaction) is null)
            {
                throw new InvalidOperationException($"Failed to add track file to video {video.Id}");
            }
        }
        else if (existing.HashAlgorithm.Name is HashAlgorithm.Names.Placeholder)
        {
            var hash = await hashAlgorithm.ComputeHashAsync(chaptersFile, cancellationToken);

            existing.ModifiedAt = _clock.GetCurrentInstant();
            existing.ModifiedByUserId = userId;
            existing.Hash = hash;
            existing.HashAlgorithm = hashAlgorithm;
            existing.StorageSize = chaptersFile.Length;

            if (await _trackFileRepository.UpdateAsync(existing, transaction) is not 1)
            {
                throw new InvalidOperationException($"Failed to update track file for video {video.Id}");
            }
        }
    }
}
