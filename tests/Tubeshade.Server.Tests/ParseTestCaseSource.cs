using System.Collections;
using System.Collections.Generic;
using NodaTime;
using NUnit.Framework;

namespace Tubeshade.Server.Tests;

public sealed class ParseTestCaseSource : IEnumerable<TestCaseData<string, Period>>
{
    /// <inheritdoc />
    public IEnumerator<TestCaseData<string, Period>> GetEnumerator()
    {
        yield return new("1d 9h 1m 30s", new PeriodBuilder { Days = 1, Hours = 9, Minutes = 1, Seconds = 30 }.Build());
        yield return new("1d9h1m30s", new PeriodBuilder { Days = 1, Hours = 9, Minutes = 1, Seconds = 30 }.Build());
        yield return new("3h 1m 30s", new PeriodBuilder { Hours = 3, Minutes = 1, Seconds = 30 }.Build());
        yield return new("3h1m30s", new PeriodBuilder { Hours = 3, Minutes = 1, Seconds = 30 }.Build());
        yield return new("1m 30s", new PeriodBuilder { Minutes = 1, Seconds = 30 }.Build());
        yield return new("1m30s", new PeriodBuilder { Minutes = 1, Seconds = 30 }.Build());
        yield return new("10s", new PeriodBuilder { Seconds = 10 }.Build());
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}