using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Tests.Pages.Shared;

public sealed class StringExtensionsTests
{
    [TestCaseSource(typeof(LinesTestCaseSource))]
    public void LineParsing(string text, int lineCount, string[] lines, int nonEmptyLineCount, string[] nonEmptyLines)
    {
        using var scope = new AssertionScope();
        text.GetLineCount().Should().Be(lineCount);
        text.GetLines().Should().BeEquivalentTo(lines);

        text.GetNonEmptyLineCount().Should().Be(nonEmptyLineCount);
        text.GetNonEmptyLines().Should().BeEquivalentTo(nonEmptyLines);
    }
}
