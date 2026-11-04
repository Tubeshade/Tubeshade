using System;
using System.Linq;
using AwesomeAssertions;
using NodaTime;
using NUnit.Framework;
using SponsorBlock;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Server.Pages.Videos;

namespace Tubeshade.Server.Tests.Pages.Videos;

public sealed class VideoEntityExtensionsTests
{
    [Test]
    public void MapToModels_ShouldOnlySubtractSkippedSegments()
    {
        var channel = new ChannelEntity
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            StoragePath = string.Empty,
            ExternalId = string.Empty,
            ExternalUrl = string.Empty,
            Availability = ExternalAvailability.Public,
        };

        var video = new DetailedVideo
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            Description = string.Empty,
            Categories = [],
            Tags = [],
            Type = VideoType.Video,
            ChannelId = channel.Id,
            StoragePath = string.Empty,
            ExternalId = string.Empty,
            ExternalUrl = string.Empty,
            PublishedAt = Instant.MinValue,
            RefreshedAt = Instant.MinValue,
            Availability = ExternalAvailability.Public,
            Duration = Period.FromSeconds(60),
        };

        SponsorBlockSegmentEntity[] segments =
        [
            CreateSegment(video.Id, 10, 20, SegmentCategory.Sponsor, SegmentAction.Skip),
            CreateSegment(video.Id, 30, 40, SegmentCategory.Sponsor, SegmentAction.Mute),
            CreateSegment(video.Id, 40, 50, SegmentCategory.Filler, SegmentAction.Skip),
            CreateSegment(Guid.NewGuid(), 0, 60, SegmentCategory.Sponsor, SegmentAction.Skip),
        ];

        var model = new[] { video }.MapToModels(segments, [channel]).Single();

        model.ActualDuration.Should().Be(Period.FromSeconds(50));
    }

    private static SponsorBlockSegmentEntity CreateSegment(
        Guid videoId,
        decimal startTime,
        decimal endTime,
        SegmentCategory category,
        SegmentAction action) => new()
    {
        VideoId = videoId,
        ExternalId = string.Empty,
        StartTime = startTime,
        EndTime = endTime,
        Category = category,
        Action = action,
        Locked = true,
    };
}