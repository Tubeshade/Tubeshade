using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Pages.Shared;

public static class SegmentEntityExtensions
{
    public static Period GetTotalDuration(this IEnumerable<SponsorBlockSegmentEntity> segments)
    {
        var mergedSegments = new Stack<SponsorBlockSegmentEntity>();

        foreach (var (index, segment) in segments.OrderBy(segment => segment.StartTime).Index())
        {
            if (index is 0)
            {
                mergedSegments.Push(segment);
                continue;
            }

            var current = mergedSegments.Peek();
            if (segment.StartTime > current.EndTime)
            {
                mergedSegments.Push(segment);
            }
            else
            {
                current.EndTime = Math.Max(current.EndTime, segment.EndTime);
            }
        }

        var segmentDurationSeconds = mergedSegments.Sum(segment => segment.EndTime - segment.StartTime);
        return Period.FromMilliseconds((long)Math.Truncate(segmentDurationSeconds * 1000));
    }
}
