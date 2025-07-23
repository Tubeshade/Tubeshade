using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SponsorBlock.Internal;

namespace SponsorBlock;

public sealed class SponsorBlockClient : ISponsorBlockClient
{
    private const int MaxVideoIdLength = 16;
    private static readonly UTF8Encoding Encoding = new(false);

    private readonly HttpClient _httpClient;

    public SponsorBlockClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public Task<VideoSegment[]> GetSegments(string videoId, CancellationToken cancellationToken = default)
    {
        return GetSegments(videoId, SegmentFilter.Default, cancellationToken);
    }

    /// <inheritdoc />
    public Task<VideoSegment[]> GetSegmentsPrivacy(string videoId, CancellationToken cancellationToken = default)
    {
        return GetSegmentsPrivacy(videoId, SegmentFilter.Default, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VideoSegment[]> GetSegments(
        string videoId,
        SegmentFilter filter,
        CancellationToken cancellationToken = default)
    {
        var queryParameters = filter.ToQueryParameters(videoId);

        using var response = await _httpClient.GetAsync($"/api/skipSegments?{queryParameters}", cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode is not HttpStatusCode.OK)
        {
            throw new HttpRequestException(content);
        }

        var segments = JsonSerializer.Deserialize(content, SerializerContext.Default.VideoSegmentModelArray);
        return segments?
            .Select(model => new VideoSegment
            {
                Id = model.Id,
                StartTime = model.Segment[0],
                EndTime = model.Segment[1],
                Category = model.Category,
                VideoDuration = model.VideoDuration,
                Action = model.Action,
                Locked = model.Locked is 0,
                Votes = model.Votes,
                Description = model.Description,
            })
            .ToArray() ?? [];
    }

    /// <inheritdoc />
    public async Task<VideoSegment[]> GetSegmentsPrivacy(
        string videoId,
        SegmentFilter filter,
        CancellationToken cancellationToken = default)
    {
        // Since stackalloc is used based on videoId length, need to check that it is not too long
        ArgumentOutOfRangeException.ThrowIfGreaterThan(videoId.Length, MaxVideoIdLength);

        Span<byte> videoIdBytes = stackalloc byte[Encoding.GetByteCount(videoId)];
        Encoding.GetBytes(videoId, videoIdBytes);

        Span<byte> hashedVideoIdBytes = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(videoIdBytes, hashedVideoIdBytes);

        var path = Convert.ToHexString(hashedVideoIdBytes[..2]);
        var queryParameters = filter.ToQueryParameters(videoId);

        using var response = await _httpClient.GetAsync($"/api/skipSegments/{path}?{queryParameters}", cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode is not HttpStatusCode.OK)
        {
            throw new HttpRequestException(content);
        }

        var videoModels = JsonSerializer.Deserialize(content, SerializerContext.Default.VideoModelArray);
        return videoModels?
            .Single(model => model.VideoId == videoId)
            .Segments
            .Select(model => new VideoSegment
            {
                Id = model.Id,
                StartTime = model.Segment[0],
                EndTime = model.Segment[1],
                Category = model.Category,
                VideoDuration = model.VideoDuration,
                Action = model.Action,
                Locked = model.Locked is 0,
                Votes = model.Votes,
                Description = model.Description,
            })
            .ToArray() ?? [];
    }

    /// <inheritdoc />
    public Task ViewedSegment(Guid segmentId)
    {
        throw new NotImplementedException();
    }
}
