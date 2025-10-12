using FluentAssertions;
using NodaTime;
using NUnit.Framework;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Tests.Services;

public sealed class StringExtensionsTests
{
    [TestCaseSource(typeof(ChaptersTestCaseSource))]
    public void TryExtractChapters(string? description, Period duration, TextTrackCue[]? expectedChapters)
    {
        _ = description.TryExtractChapters(duration, out var chapters);

        chapters.Should().BeEquivalentTo(expectedChapters);
    }
}
