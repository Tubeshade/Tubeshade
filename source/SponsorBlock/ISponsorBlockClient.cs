using System;
using System.Threading;
using System.Threading.Tasks;

namespace SponsorBlock;

public interface ISponsorBlockClient
{
    Task<VideoSegment[]> GetSegments(string videoId, CancellationToken cancellationToken = default);

    Task<VideoSegment[]> GetSegmentsPrivacy(string videoId, CancellationToken cancellationToken = default);

    Task<VideoSegment[]> GetSegments(
        string videoId,
        SegmentFilter filter,
        CancellationToken cancellationToken = default);

    Task<VideoSegment[]> GetSegmentsPrivacy(
        string videoId,
        SegmentFilter filter,
        CancellationToken cancellationToken = default);

    Task ViewedSegment(Guid segmentId);
}
