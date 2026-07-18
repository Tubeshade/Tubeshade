using System.Collections;
using System.Collections.Generic;
using NodaTime;
using NUnit.Framework;

namespace Tubeshade.Server.Tests;

public sealed class FormatTestCaseSource : IEnumerable<TestCaseData<Period, int, string>>
{
    /// <inheritdoc />
    public IEnumerator<TestCaseData<Period, int, string>> GetEnumerator()
    {
        yield return new(new PeriodBuilder { Seconds = 10 }.Build(), 2, "10s");
        yield return new(new PeriodBuilder { Seconds = 10 }.Build(), 4, "10s");
        yield return new(new PeriodBuilder { Minutes = 1 }.Build(), 2, "1m");
        yield return new(new PeriodBuilder { Minutes = 1, Seconds = 30 }.Build(), 2, "1m 30s");
        yield return new(new PeriodBuilder { Minutes = 1, Seconds = 30 }.Build(), 4, "1m 30s");
        yield return new(new PeriodBuilder { Hours = 3 }.Build(), 4, "3h");
        yield return new(new PeriodBuilder { Minutes = 59, Seconds = 59 }.Build(), 1, "1h");
        yield return new(new PeriodBuilder { Hours = 3, Minutes = 1, Seconds = 30 }.Build(), 2, "3h 2m");
        yield return new(new PeriodBuilder { Hours = 3, Minutes = 59, Seconds = 30 }.Build(), 2, "4h");
        yield return new(new PeriodBuilder { Hours = 3, Minutes = 59, Seconds = 59 }.Build(), 2, "4h");
        yield return new(new PeriodBuilder { Hours = 3, Minutes = 59, Seconds = 30 }.Build(), 1, "4h");
        yield return new(new PeriodBuilder { Hours = 3, Minutes = 59, Seconds = 59 }.Build(), 1, "4h");
        yield return new(new PeriodBuilder { Days = 1, Hours = 11 }.Build(), 1, "1d");
        yield return new(new PeriodBuilder { Days = 1, Hours = 13 }.Build(), 1, "2d");
        yield return new(new PeriodBuilder { Days = 1, Hours = 23, Minutes = 59 }.Build(), 2, "2d");
        yield return new(new PeriodBuilder { Days = 1, Hours = 23, Minutes = 59, Seconds = 30 }.Build(), 2, "2d");
        yield return new(new PeriodBuilder { Days = 1, Hours = 23, Minutes = 59, Seconds = 30 }.Build(), 3, "2d");
        yield return new(new PeriodBuilder { Days = 1, Hours = 9, Minutes = 1, Seconds = 30 }.Build(), 2, "1d 9h");
        yield return new(new PeriodBuilder { Days = 1, Hours = 9, Minutes = 1, Seconds = 30 }.Build(), 3, "1d 9h 2m");
        yield return new(new PeriodBuilder { Days = 1, Hours = 9, Minutes = 1, Seconds = 30 }.Build(), 4, "1d 9h 1m 30s");
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
