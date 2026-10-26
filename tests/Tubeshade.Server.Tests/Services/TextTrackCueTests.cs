using System;
using AwesomeAssertions;
using NUnit.Framework;
using SponsorBlock;
using Tubeshade.Data.Media;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Tests.Services;

public sealed class TextTrackCueTests
{
    [Test]
    public void FromSponsorBlockSegment_ShouldIncludeCategoryAndAction()
    {
        var segment = new SponsorBlockSegmentEntity
        {
            VideoId = Guid.Empty,
            ExternalId = string.Empty,
            StartTime = 1.5m,
            EndTime = 3m,
            Category = SegmentCategory.Sponsor,
            Action = SegmentAction.Mute,
            Locked = false,
        };

        TextTrackCue.FromSponsorBlockSegment(segment).Text.Should().Be("sponsor mute");
    }
}
