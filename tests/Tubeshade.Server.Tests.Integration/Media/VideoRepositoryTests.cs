using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Npgsql;
using NUnit.Framework;
using SponsorBlock;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Media;

public sealed class VideoRepositoryTests(ServerFixture fixture) : ServerTests(fixture)
{
    private Guid _libraryId;
    private Guid _channelId;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();


        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            _libraryId = await CreateLibrary(User.Id, scope.ServiceProvider, transaction);

            var channelRepository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();
            _channelId = (await channelRepository.AddAsync(
                new()
                {
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
                    Name = Guid.NewGuid().ToString("N"),
                    StoragePath = string.Empty,
                    ExternalId = string.Empty,
                    ExternalUrl = string.Empty,
                    Availability = ExternalAvailability.Public,
                },
                transaction))!.Value;

            await channelRepository.AddToLibrary(_libraryId, _channelId, User.Id, transaction);

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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
                    VideoId = notViewedId,
                    StoragePath = string.Empty,
                    Type = VideoContainerType.Mp4,
                    Width = 0,
                    Height = 0,
                    Framerate = 0,
                    DownloadedByUserId = User.Id,
                    DownloadedAt = SystemClock.Instance.GetCurrentInstant(),
                },
                transaction);

            viewedId = (await repository.AddAsync(
                new()
                {
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
                    VideoId = viewedId,
                    StoragePath = string.Empty,
                    Type = VideoContainerType.Mp4,
                    Width = 0,
                    Height = 0,
                    Framerate = 0,
                    DownloadedByUserId = User.Id,
                    DownloadedAt = SystemClock.Instance.GetCurrentInstant(),
                },
                transaction);

            await repository.SetWatched(viewedId, User.Id, true, transaction);

            await transaction.CommitAsync();
        }

        var allVideos = await repository.GetFilteredDetailed(
            new VideoParameters
            {
                SortBy = SortVideoBy.PublishedAt,
                SortDirection = SortDirection.Descending,
                UserId = User.Id,
                LibraryId = _libraryId,
                Limit = 24,
                Offset = 0,
                Viewed = null,
            });

        var viewedVideos = await repository.GetFilteredDetailed(
            new VideoParameters
            {
                SortBy = SortVideoBy.PublishedAt,
                SortDirection = SortDirection.Descending,
                UserId = User.Id,
                LibraryId = _libraryId,
                Limit = 24,
                Offset = 0,
                Viewed = ViewStatus.Viewed,
            });

        var notViewedVideos = await repository.GetFilteredDetailed(
            new VideoParameters
            {
                SortBy = SortVideoBy.PublishedAt,
                SortDirection = SortDirection.Descending,
                UserId = User.Id,
                LibraryId = _libraryId,
                Limit = 24,
                Offset = 0,
                Viewed = ViewStatus.NotViewed,
            });

        using var assertionScope = new AssertionScope();
        allVideos.Select(video => video.PublicId).Should().Contain([notViewedId, viewedId]);
        viewedVideos.Should().Contain(video => video.PublicId == viewedId).And.NotContain(video => video.PublicId == notViewedId);
        notViewedVideos.Should().Contain(video => video.PublicId == notViewedId).And.NotContain(video => video.PublicId == viewedId);
    }

    [Test]
    public async Task PlaybackStatistics()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<VideoRepository>();

        await using var transaction = await connection.OpenAndBeginTransaction();
        var videoId = await AddVideo(repository, transaction);

        var initialStatistics = await repository.GetPlaybackStatistics(videoId, User.Id, transaction);

        await repository.SavePlaybackActivity(videoId, User.Id, 30, 15, 30, transaction);
        await repository.SavePlaybackActivity(videoId, User.Id, 60, 15.5, 31, transaction);
        await repository.AddPlay(videoId, User.Id, transaction);
        await repository.AddPlay(videoId, User.Id, transaction);

        var statistics = await repository.GetPlaybackStatistics(videoId, User.Id, transaction);
        var video = await repository.GetAsync(videoId, User.Id, transaction);

        using (new AssertionScope())
        {
            initialStatistics.PlayCount.Should().Be(0);
            initialStatistics.WatchTime.Should().Be(0);
            initialStatistics.PlaybackTime.Should().Be(0);

            statistics.PlayCount.Should().Be(2);
            statistics.WatchTime.Should().Be(30.5);
            statistics.PlaybackTime.Should().Be(61);

            video.Position.Should().Be(60);
            video.Viewed.Should().BeFalse();
        }
    }

    [Test]
    public async Task PlaybackWithoutAccess()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<VideoRepository>();
        var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();

        await using var transaction = await connection.OpenAndBeginTransaction();
        var videoId = await AddVideo(repository, transaction);

        var userName = Guid.NewGuid().ToString("N");
        var otherUser = new UserEntity { PublicId = Guid.NewGuid(), Name = userName, NormalizedName = userName };
        await userRepository.AddAsync(otherUser, transaction);
        var otherUserId = otherUser.Id;

        using (new AssertionScope())
        {
            (await repository.SavePlaybackActivity(videoId, otherUserId, 30, 15, 30, transaction)).Should().Be(0);
            (await repository.AddPlay(videoId, otherUserId, transaction)).Should().Be(0);
            (await repository.SetWatched(videoId, otherUserId, true, transaction)).Should().Be(0);
            (await repository.SetWatched(videoId, otherUserId, false, transaction)).Should().Be(0);
            (await repository.SavePlaybackActivity(Guid.NewGuid(), User.Id, 30, 15, 30, transaction)).Should().Be(0);
            (await repository.AddPlay(Guid.NewGuid(), User.Id, transaction)).Should().Be(0);
            (await repository.SetWatched(Guid.NewGuid(), User.Id, true, transaction)).Should().Be(0);
            (await repository.SetWatched(Guid.NewGuid(), User.Id, false, transaction)).Should().Be(0);

            // marking a video that was never watched as not watched still succeeds
            (await repository.SetWatched(videoId, User.Id, false, transaction)).Should().Be(1);
            (await repository.SetWatched(videoId, User.Id, true, transaction)).Should().Be(1);
            (await repository.SavePlaybackActivity(videoId, User.Id, 30, 15, 30, transaction)).Should().Be(1);
            (await repository.AddPlay(videoId, User.Id, transaction)).Should().Be(1);
        }
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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
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
            var without = await repository.GetWithoutSegments(User.Id, _libraryId, transaction);
            var unlocked = await repository.GetWithUnlockedSegments(User.Id, _libraryId, transaction);

            without.Select(id => id.Id).Should().BeEquivalentTo([withoutSegments]);
            unlocked.Select(id => id.Id).Should().BeEquivalentTo([withUnlockedSegments, withMixedSegments]);
        }
    }

    [Test]
    public async Task SortByQuery()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<VideoRepository>();
        var fileRepository = scope.ServiceProvider.GetRequiredService<VideoFileRepository>();

        Guid nameId;
        Guid descriptionId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            nameId = (await repository.AddAsync(
                new()
                {
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
                    Name = "Lorem ipsum dolor sit amet, consectetur adipiscing elit",
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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
                    VideoId = nameId,
                    StoragePath = string.Empty,
                    Type = VideoContainerType.Mp4,
                    Width = 0,
                    Height = 0,
                    Framerate = 0,
                    DownloadedByUserId = User.Id,
                    DownloadedAt = SystemClock.Instance.GetCurrentInstant(),
                },
                transaction);

            descriptionId = (await repository.AddAsync(
                new()
                {
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
                    Name = Guid.NewGuid().ToString("N"),
                    Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit",
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
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
                    VideoId = descriptionId,
                    StoragePath = string.Empty,
                    Type = VideoContainerType.Mp4,
                    Width = 0,
                    Height = 0,
                    Framerate = 0,
                    DownloadedByUserId = User.Id,
                    DownloadedAt = SystemClock.Instance.GetCurrentInstant(),
                },
                transaction);

            await repository.SetWatched(descriptionId, User.Id, true, transaction);

            await transaction.CommitAsync();
        }

        var descending = await repository.GetFilteredDetailed(
            new VideoParameters
            {
                SortBy = SortVideoBy.Query,
                SortDirection = SortDirection.Descending,
                UserId = User.Id,
                LibraryId = _libraryId,
                Limit = 24,
                Offset = 0,
                Query = "Lorem ipsum",
            });

        var ascending = await repository.GetFilteredDetailed(
            new VideoParameters
            {
                SortBy = SortVideoBy.Query,
                SortDirection = SortDirection.Ascending,
                UserId = User.Id,
                LibraryId = _libraryId,
                Limit = 24,
                Offset = 0,
                Query = "Lorem ipsum",
            });

        using var assertionScope = new AssertionScope();
        descending.Select(video => video.PublicId).Should().Equal(nameId, descriptionId);
        ascending.Select(video => video.PublicId).Should().Equal(descriptionId, nameId);
    }

    private async Task<Guid> AddVideo(VideoRepository repository, NpgsqlTransaction transaction)
    {
        return (await repository.AddAsync(
            new()
            {
                CreatedByUserId = User.Id,
                ModifiedByUserId = User.Id,
                OwnerId = User.OwnerId,
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
    }
}
