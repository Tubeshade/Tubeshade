using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace SponsorBlock.Tests.Integration;

public sealed class SponsorBlockClientTests
{
    [Test]
    public async Task GetSegments_ShouldThrow()
    {
        await using var scope = TestConfiguration.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<ISponsorBlockClient>();

        await FluentActions
            .Awaiting(() => client.GetSegmentsPrivacy("12345678901234567"))
            .Should()
            .ThrowExactlyAsync<ArgumentOutOfRangeException>()
            .WithMessage(
                """
                videoId.Length ('17') must be less than or equal to '16'. (Parameter 'videoId.Length')
                Actual value was 17.
                """);
    }

    [Test]
    public async Task GetSegments()
    {
        const string videoId = "F74mz8AVBxI";
        var expectedSegments = new VideoSegment[]
        {
            new()
            {
                Id = "c8829b05cd369f16e76b42d6ebcbba36368f6acf108dd0f74aeaa12a31e1a5f77",
                StartTime = 88.18m,
                EndTime = 101.476m,
                Category = SegmentCategory.SelfPromotion,
                VideoDuration = 914.241m,
                Action = SegmentAction.Skip,
                Locked = true,
                Votes = -1,
                Description = ""
            },
            new()
            {
                Id = "361f9501e3e5d0a7c2f9071ff33f390581e5cf445037a443805e4f314a22a4387",
                StartTime = 787.171m,
                EndTime = 797.592m,
                Category = SegmentCategory.SelfPromotion,
                VideoDuration = 914.241m,
                Action = SegmentAction.Skip,
                Locked = true,
                Votes = 2,
                Description = ""
            },
            new()
            {
                Id = "f07f9ed7daedc354b94be70adb19ab824d0a57d8426a8a2f24d53e1a238df5ac7",
                StartTime = 88.555m,
                EndTime = 101.408m,
                Category = SegmentCategory.Interaction,
                VideoDuration = 914.241m,
                Action = SegmentAction.Skip,
                Locked = true,
                Votes = 2,
                Description = ""
            },
            new()
            {
                Id = "350a6fd0bfe9c0bfa49a99d0a966f723a4a328db13a1b8a4b8d348154b1548407",
                StartTime = 290.179m,
                EndTime = 298.833m,
                Category = SegmentCategory.Filler,
                VideoDuration = 914.285m,
                Action = SegmentAction.Skip,
                Locked = true,
                Votes = 1,
                Description = ""
            },
            new()
            {
                Id = "e01d0dad7ebc4d3550ad189ebc3a5aa2b8f193673ba59e6477d391f493df70c57",
                StartTime = 910.947m,
                EndTime = 914.233m,
                Category = SegmentCategory.Interaction,
                VideoDuration = 914.233m,
                Action = SegmentAction.Skip,
                Locked = true,
                Votes = 1,
                Description = ""
            },
        };

        await using var scope = TestConfiguration.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<ISponsorBlockClient>();

        var segments = await client.GetSegments(videoId);
        var segmentsPrivacy = await client.GetSegmentsPrivacy(videoId);

        using (new AssertionScope())
        {
            segments.Should().BeEquivalentTo(expectedSegments);
            segments.Should().BeEquivalentTo(segmentsPrivacy);
        }
    }
}
