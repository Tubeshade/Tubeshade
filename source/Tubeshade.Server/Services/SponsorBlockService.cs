using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using SponsorBlock;
using Tubeshade.Data;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Services;

public sealed class SponsorBlockService
{
    private readonly ILogger<SponsorBlockService> _logger;
    private readonly IClock _clock;
    private readonly ISponsorBlockClient _client;
    private readonly NpgsqlConnection _connection;
    private readonly SponsorBlockSegmentRepository _segmentRepository;
    private readonly VideoRepository _videoRepository;

    public SponsorBlockService(
        ILogger<SponsorBlockService> logger,
        IClock clock,
        ISponsorBlockClient client,
        NpgsqlConnection connection,
        SponsorBlockSegmentRepository segmentRepository,
        VideoRepository videoRepository)
    {
        _logger = logger;
        _clock = clock;
        _client = client;
        _connection = connection;
        _segmentRepository = segmentRepository;
        _videoRepository = videoRepository;
    }

    public ValueTask ScanVideoSegments(
        Guid libraryId,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        CancellationToken cancellationToken = default)
    {
        return UpdateVideoSegmentsCore(
            userId,
            taskRepository,
            taskRunId,
            (videoRepository, transaction) => videoRepository.GetWithoutSegments(userId, libraryId, transaction),
            cancellationToken);
    }

    public ValueTask UpdateVideoSegments(
        Guid libraryId,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        CancellationToken cancellationToken = default)
    {
        return UpdateVideoSegmentsCore(
            userId,
            taskRepository,
            taskRunId,
            (videoRepository, transaction) => videoRepository.GetWithUnlockedSegments(userId, libraryId, transaction),
            cancellationToken);
    }

    private async ValueTask UpdateVideoSegmentsCore(
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        Func<VideoRepository, NpgsqlTransaction, ValueTask<List<EntityId>>> idFunc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var videos = await idFunc(_videoRepository, transaction);

        var totalCount = videos.Count;
        var startTime = _clock.GetCurrentInstant();

        await taskRepository.InitializeTaskProgress(taskRunId, totalCount);

        foreach (var (index, videoId) in videos.Index())
        {
            await UpdateVideoSegments(videoId.Id, videoId.ExternalId, userId, transaction, cancellationToken);

            var currentIndex = index + 1;
            var (rate, period) = _clock.GetRemainingEstimate(startTime, totalCount, currentIndex);
            await taskRepository.UpdateProgress(taskRunId, currentIndex, rate, period);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public ValueTask UpdateVideoSegments(
        VideoEntity video,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        return UpdateVideoSegments(video.Id, video.ExternalId, userId, transaction, cancellationToken);
    }

    public async ValueTask UpdateVideoSegments(
        Guid videoId,
        string videoExternalId,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var segments = await _client.GetSegmentsPrivacy(videoExternalId, cancellationToken);
        var existingSegments = await _segmentRepository.GetForVideo(videoId, userId, transaction);

        foreach (var segment in segments)
        {
            if (existingSegments.SingleOrDefault(entity => entity.ExternalId == segment.Id) is { } existingSegment)
            {
                existingSegments.Remove(existingSegment);
                if (existingSegment.Locked)
                {
                    _logger.LockedSponsorBlockSegment(existingSegment.Id, existingSegment.ExternalId);
                    continue;
                }

                existingSegment.ModifiedByUserId = userId;
                existingSegment.StartTime = segment.StartTime;
                existingSegment.EndTime = segment.EndTime;
                existingSegment.Category = segment.Category;
                existingSegment.Action = segment.Action;
                existingSegment.Description = segment.Description;
                existingSegment.Locked = segment.Locked;

                _logger.UpdatingSponsorBlockSegment(existingSegment.Id, existingSegment.ExternalId);
                await _segmentRepository.UpdateAsync(existingSegment, transaction);
            }
            else
            {
                _logger.AddingSponsorBlockSegment(segment.Id);
                await _segmentRepository.AddAsync(
                    new SponsorBlockSegmentEntity
                    {
                        CreatedByUserId = userId,
                        ModifiedByUserId = userId,
                        VideoId = videoId,
                        ExternalId = segment.Id,
                        StartTime = segment.StartTime,
                        EndTime = segment.EndTime,
                        Category = segment.Category,
                        Action = segment.Action,
                        Description = segment.Description,
                        Locked = segment.Locked,
                    },
                    transaction);
            }
        }

        foreach (var segment in existingSegments)
        {
            _logger.DeletingSponsorBlockSegment(segment.Id, segment.ExternalId);
            await _segmentRepository.DeleteAsync(segment, transaction);
        }
    }
}
