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
        var segmentDurationSeconds = 0m;
        var end = decimal.MinValue;

        foreach (var segment in segments.OrderBy(segment => segment.StartTime))
        {
            var start = Math.Max(segment.StartTime, end);
            if (segment.EndTime <= start)
            {
                continue;
            }

            segmentDurationSeconds += segment.EndTime - start;
            end = segment.EndTime;
        }

        return Period.FromMilliseconds((long)Math.Truncate(segmentDurationSeconds * 1000));
    }
}
