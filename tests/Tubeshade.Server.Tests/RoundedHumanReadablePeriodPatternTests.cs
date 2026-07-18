using FluentAssertions;
using NodaTime;
using NUnit.Framework;

namespace Tubeshade.Server.Tests;

public sealed class RoundedHumanReadablePeriodPatternTests
{
    [TestCaseSource(typeof(FormatTestCaseSource))]
    public void Format_ShouldReturnExpected(Period period, int places, string expected)
    {
        new RoundedHumanReadablePeriodPattern(places).Format(period).Should().Be(expected);
    }
}
