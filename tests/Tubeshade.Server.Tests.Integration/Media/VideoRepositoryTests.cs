using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Npgsql;
using NUnit.Framework;
using SponsorBlock;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Media;

public sealed class VideoRepositoryTests(ServerFixture fixture) : ServerTests(fixture)
{
    private Guid _userId;
    private Guid _libraryId;
    private Guid _channelId;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
            _userId = await userRepository.GetSystemUserId(transaction);
            await transaction.CommitAsync();
        }

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            _libraryId = await CreateLibrary(_userId, scope.ServiceProvider, transaction);

            var channelRepository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();
            _channelId = (await channelRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    StoragePath = string.Empty,
                    ExternalId = string.Empty,
                    ExternalUrl = string.Empty,
                    Availability = ExternalAvailability.Public,
                },
                transaction))!.Value;

            await channelRepository.AddToLibrary(_libraryId, _channelId, transaction);

            await transaction.CommitAsync();
        }
    }

    [Test]
    public async Task ViewedStatus()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<VideoRepository>();
        var fileRepository = scope.ServiceProvider.GetRequiredService<VideoFileRepository>();

        Guid notViewedId;
        Guid viewedId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            notViewedId = (await repository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    Description = string.Empty,
                    Categories = [],
                    Tags = [],
                    Type = VideoType.Video,
                    ChannelId = _channelId,
                    StoragePath = string.Empty,
                    ExternalId = string.Empty,
                    ExternalUrl = "https://example.org",
                    PublishedAt = SystemClock.Instance.GetCurrentInstant(),
                    RefreshedAt = SystemClock.Instance.GetCurrentInstant(),
                    Availability = ExternalAvailability.Public,
                    Duration = Period.FromMinutes(10),
                },
                transaction))!.Value;

            await fileRepository.AddAsync(
                new VideoFileEntity
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    VideoId = notViewedId,
                    StoragePath = string.Empty,
                    Type = VideoContainerType.Mp4,
                    Width = 0,
                    Height = 0,
                    Framerate = 0,
                    DownloadedByUserId = _userId,
                    DownloadedAt = SystemClock.Instance.GetCurrentInstant(),
                },
                transaction);

            viewedId = (await repository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    Description = string.Empty,
                    Categories = [],
                    Tags = [],
                    Type = VideoType.Video,
                    ChannelId = _channelId,
                    StoragePath = string.Empty,
                    ExternalId = string.Empty,
                    ExternalUrl = "https://example.org",
                    PublishedAt = SystemClock.Instance.GetCurrentInstant(),
                    RefreshedAt = SystemClock.Instance.GetCurrentInstant(),
                    Availability = ExternalAvailability.Public,
                    Duration = Period.FromMinutes(10),
                },
                transaction))!.Value;

            await fileRepository.AddAsync(
                new VideoFileEntity
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    VideoId = viewedId,
                    StoragePath = string.Empty,
                    Type = VideoContainerType.Mp4,
                    Width = 0,
                    Height = 0,
                    Framerate = 0,
                    DownloadedByUserId = _userId,
                    DownloadedAt = SystemClock.Instance.GetCurrentInstant(),
                },
                transaction);

            await repository.MarkAsWatched(viewedId, _userId, transaction);

            await transaction.CommitAsync();
        }

        var allVideos = await repository.GetFiltered(
            new VideoParameters
            {
                SortBy = SortVideoBy.PublishedAt,
                SortDirection = SortDirection.Descending,
                UserId = _userId,
                LibraryId = _libraryId,
                Limit = 24,
                Offset = 0,
                Viewed = null,
            });

        var viewedVideos = await repository.GetFiltered(
            new VideoParameters
            {
                SortBy = SortVideoBy.PublishedAt,
                SortDirection = SortDirection.Descending,
                UserId = _userId,
                LibraryId = _libraryId,
                Limit = 24,
                Offset = 0,
                Viewed = true,
            });

        var notViewedVideos = await repository.GetFiltered(
            new VideoParameters
            {
                SortBy = SortVideoBy.PublishedAt,
                SortDirection = SortDirection.Descending,
                UserId = _userId,
                LibraryId = _libraryId,
                Limit = 24,
                Offset = 0,
                Viewed = false,
            });

        using var assertionScope = new AssertionScope();
        allVideos.Select(video => video.Id).Should().Contain([notViewedId, viewedId]);
        viewedVideos.Should().Contain(video => video.Id == viewedId).And.NotContain(video => video.Id == notViewedId);
        notViewedVideos.Should().Contain(video => video.Id == notViewedId).And.NotContain(video => video.Id == viewedId);
    }

    [Test]
    public async Task GetWithUnlockedSegments()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<VideoRepository>();
        var segmentRepository = scope.ServiceProvider.GetRequiredService<SponsorBlockSegmentRepository>();

        Guid withoutSegments;
        Guid withUnlockedSegments;
        Guid withMixedSegments;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            withoutSegments = (await repository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    Description = string.Empty,
                    Categories = [],
                    Tags = [],
                    Type = VideoType.Video,
                    ChannelId = _channelId,
                    StoragePath = string.Empty,
                    ExternalId = string.Empty,
                    ExternalUrl = "https://example.org",
                    PublishedAt = SystemClock.Instance.GetCurrentInstant(),
                    RefreshedAt = SystemClock.Instance.GetCurrentInstant(),
                    Availability = ExternalAvailability.Public,
                    Duration = Period.FromMinutes(10),
                },
                transaction))!.Value;

            withUnlockedSegments = (await repository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    Description = string.Empty,
                    Categories = [],
                    Tags = [],
                    Type = VideoType.Video,
                    ChannelId = _channelId,
                    StoragePath = string.Empty,
                    ExternalId = string.Empty,
                    ExternalUrl = "https://example.org",
                    PublishedAt = SystemClock.Instance.GetCurrentInstant(),
                    RefreshedAt = SystemClock.Instance.GetCurrentInstant(),
                    Availability = ExternalAvailability.Public,
                    Duration = Period.FromMinutes(10),
                },
                transaction))!.Value;

            await segmentRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    VideoId = withUnlockedSegments,
                    ExternalId = Guid.NewGuid().ToString(),
                    StartTime = 0,
                    EndTime = 0,
                    Category = SegmentCategory.Sponsor,
                    Action = SegmentAction.Skip,
                    Locked = false,
                },
                transaction);

            withMixedSegments = (await repository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    Description = string.Empty,
                    Categories = [],
                    Tags = [],
                    Type = VideoType.Video,
                    ChannelId = _channelId,
                    StoragePath = string.Empty,
                    ExternalId = string.Empty,
                    ExternalUrl = "https://example.org",
                    PublishedAt = SystemClock.Instance.GetCurrentInstant(),
                    RefreshedAt = SystemClock.Instance.GetCurrentInstant(),
                    Availability = ExternalAvailability.Public,
                    Duration = Period.FromMinutes(10),
                },
                transaction))!.Value;

            await segmentRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    VideoId = withMixedSegments,
                    ExternalId = Guid.NewGuid().ToString(),
                    StartTime = 0,
                    EndTime = 0,
                    Category = SegmentCategory.Sponsor,
                    Action = SegmentAction.Skip,
                    Locked = false,
                },
                transaction);

            await segmentRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    VideoId = withMixedSegments,
                    ExternalId = Guid.NewGuid().ToString(),
                    StartTime = 0,
                    EndTime = 0,
                    Category = SegmentCategory.Sponsor,
                    Action = SegmentAction.Skip,
                    Locked = true,
                },
                transaction);

            var withLockedSegments = (await repository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    Description = string.Empty,
                    Categories = [],
                    Tags = [],
                    Type = VideoType.Video,
                    ChannelId = _channelId,
                    StoragePath = string.Empty,
                    ExternalId = string.Empty,
                    ExternalUrl = "https://example.org",
                    PublishedAt = SystemClock.Instance.GetCurrentInstant(),
                    RefreshedAt = SystemClock.Instance.GetCurrentInstant(),
                    Availability = ExternalAvailability.Public,
                    Duration = Period.FromMinutes(10),
                },
                transaction))!.Value;

            await segmentRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    VideoId = withLockedSegments,
                    ExternalId = Guid.NewGuid().ToString(),
                    StartTime = 0,
                    EndTime = 0,
                    Category = SegmentCategory.Sponsor,
                    Action = SegmentAction.Skip,
                    Locked = true,
                },
                transaction);

            await transaction.CommitAsync();
        }

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var without = await repository.GetWithoutSegments(_userId, _libraryId, transaction);
            var unlocked = await repository.GetWithUnlockedSegments(_userId, _libraryId, transaction);

            without.Select(id => id.Id).Should().BeEquivalentTo([withoutSegments]);
            unlocked.Select(id => id.Id).Should().BeEquivalentTo([withUnlockedSegments, withMixedSegments]);
        }
    }
}
