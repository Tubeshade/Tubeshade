using System;
using NodaTime;

namespace Tubeshade.Server.Services;

internal static class ClockExtensions
{
    internal static (decimal? Rate, Period? Remaining) GetRemainingEstimate(
        this IClock clock,
        Instant startTime,
        int totalCount,
        int currentIndex)
    {
        if (currentIndex is 0)
        {
            return (null, null);
        }

        var elapsedDuration = clock.GetCurrentInstant() - startTime;
        var averageDuration = elapsedDuration / currentIndex;
        var remainingCount = totalCount - currentIndex;
        var remainingNanoseconds = (long)(averageDuration * remainingCount).TotalNanoseconds;

        var remaining = new PeriodBuilder { Nanoseconds = remainingNanoseconds }.Build().Normalize();
        var rate = Math.Round(currentIndex / (decimal)elapsedDuration.TotalSeconds, 2);

        return (rate, remaining);
    }
}
