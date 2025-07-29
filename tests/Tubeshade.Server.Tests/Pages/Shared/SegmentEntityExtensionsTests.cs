using System.Collections.Generic;
using FluentAssertions;
using NodaTime;
using NUnit.Framework;
using Tubeshade.Data.Media;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Tests.Pages.Shared;

public sealed class SegmentEntityExtensionsTests
{
    [TestCaseSource(typeof(GetTotalDurationTestCaseSource))]
    public void GetTotalDuration_ShouldReturnExpected(
        IEnumerable<SponsorBlockSegmentEntity> segments,
        Period expectedDuration)
    {
        segments.GetTotalDuration().Normalize().Should().Be(expectedDuration);
    }
}
