using FluentAssertions;
using NodaTime;
using NUnit.Framework;

namespace Tubeshade.Server.Tests;

public sealed class HumanReadablePeriodPatternTests
{
    private readonly HumanReadablePeriodPattern _pattern = HumanReadablePeriodPattern.Instance;

    [TestCaseSource(typeof(ParseTestCaseSource))]
    public void Parse_ShouldReturnExpected(string text, Period expected)
    {
        _pattern.Parse(text).Value.Should().Be(expected);
    }
}
