using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Creators;
using Tubeshade.Data.Media.Playlists;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Data;

/// <summary>Runs the filtered queries with every sort column in both directions, since the sorting is selected by query parameters.</summary>
public sealed class SortingTests(ServerFixture fixture) : ServerTests(fixture)
{
    private static readonly SortDirection[] Directions = [SortDirection.Ascending, SortDirection.Descending];

    private Guid _libraryId;
    private Guid[] _channelIds = [];
    private Guid[] _videoIds = [];
    private Guid[] _playlistIds = [];

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var connection = services.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();

        var userId = User.Id;
        var now = SystemClock.Instance.GetCurrentInstant();
        _libraryId = await CreateLibrary(Guid.NewGuid().ToString("N"), userId, services, transaction);

        var channelRepository = services.GetRequiredService<ChannelRepository>();
        var videoRepository = services.GetRequiredService<VideoRepository>();
        var playlistRepository = services.GetRequiredService<PlaylistRepository>();

        var channelIds = new Guid[3];
        var videoIds = new Guid[3];
        var playlistIds = new Guid[3];
        for (var i = 0; i < 3; i++)
        {
            // names and values increase with the index, so that ascending order matches the order of creation
            channelIds[i] = (await channelRepository.AddAsync(
                new ChannelEntity
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    OwnerId = User.OwnerId,
                    Name = $"channel {(char)('a' + i)}",
                    StoragePath = string.Empty,
                    ExternalId = Guid.NewGuid().ToString("N"),
                    ExternalUrl = string.Empty,
                    Availability = ExternalAvailability.Public,
                    SubscribedAt = now + Duration.FromHours(i),
                    SubscriberCount = 10 + i,
                },
                transaction))!.Value;
            (await channelRepository.AddToLibrary(_libraryId, channelIds[i], userId, transaction)).Should().Be(1);

            videoIds[i] = (await videoRepository.AddAsync(
                new VideoEntity
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    OwnerId = User.OwnerId,
                    Name = $"video {(char)('a' + i)}",
                    Description = string.Empty,
                    Categories = [],
                    Tags = [],
                    Type = VideoType.Video,
                    ChannelId = channelIds[i],
                    StoragePath = string.Empty,
                    ExternalId = Guid.NewGuid().ToString("N"),
                    ExternalUrl = $"https://example.org/{Guid.NewGuid():N}",
                    PublishedAt = now - Duration.FromDays(10 - i),
                    RefreshedAt = now - Duration.FromDays(10 - i),
                    Availability = ExternalAvailability.Public,
                    Duration = Period.FromMinutes(10 + i),
                    ViewCount = 100 + i,
                    LikeCount = 1000 + i,
                },
                transaction))!.Value;

            playlistIds[i] = (await playlistRepository.AddAsync(
                new PlaylistEntity
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    LibraryId = _libraryId,
                    StoragePath = string.Empty,
                    ExternalId = Guid.NewGuid().ToString("N"),
                    ExternalUrl = string.Empty,
                    Name = $"playlist {(char)('a' + i)}",
                    RefreshedAt = now + Duration.FromHours(i),
                    SubscribedAt = now + Duration.FromHours(i),
                },
                transaction))!.Value;
        }

        _channelIds = channelIds;
        _videoIds = videoIds;
        _playlistIds = playlistIds;

        await transaction.CommitAsync();
    }

    [Test]
    public async Task Channels(
        [ValueSource(nameof(ChannelSorts))] SortChannelBy sortBy,
        [ValueSource(nameof(Directions))] SortDirection direction)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();

        var channels = await scope.ServiceProvider.GetRequiredService<ChannelRepository>().GetFiltered(
            new ChannelParameters
            {
                UserId = User.Id,
                LibraryId = _libraryId,
                Limit = 10,
                Offset = 0,
                SortBy = sortBy,
                SortDirection = direction,
            },
            transaction);

        channels.Select(channel => channel.PublicId).Should().BeEquivalentTo(_channelIds);
        if (sortBy == SortChannelBy.ChannelName ||
            sortBy == SortChannelBy.SubscriberCount ||
            sortBy == SortChannelBy.SubscribedAt)
        {
            channels.Select(channel => channel.PublicId).Should().Equal(Ordered(_channelIds, direction));
        }
    }

    [Test]
    public async Task Videos(
        [ValueSource(nameof(VideoSorts))] SortVideoBy sortBy,
        [ValueSource(nameof(Directions))] SortDirection direction)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<VideoRepository>();

        var parameters = new VideoParameters
        {
            UserId = User.Id,
            LibraryId = _libraryId,
            Limit = 10,
            Offset = 0,
            WithFiles = false,
            SortBy = sortBy,
            SortDirection = direction,
        };

        var videos = await repository.GetFiltered(parameters);
        var detailed = await repository.GetFilteredDetailed(parameters);

        if (sortBy == SortVideoBy.PlaylistOrder)
        {
            // without a playlist, the playlist order is always null
            videos.Select(video => video.PublicId).Should().BeEquivalentTo(_videoIds);
            detailed.Select(video => video.PublicId).Should().BeEquivalentTo(_videoIds);
            return;
        }

        videos.Select(video => video.PublicId).Should().BeEquivalentTo(_videoIds);
        detailed.Select(video => video.PublicId).Should().BeEquivalentTo(_videoIds);
        if (sortBy == SortVideoBy.PublishedAt ||
            sortBy == SortVideoBy.RefreshedAt ||
            sortBy == SortVideoBy.ViewCount ||
            sortBy == SortVideoBy.LikeCount ||
            sortBy == SortVideoBy.Duration)
        {
            videos.Select(video => video.PublicId).Should().Equal(Ordered(_videoIds, direction));
            detailed.Select(video => video.PublicId).Should().Equal(Ordered(_videoIds, direction));
        }
    }

    [Test]
    public async Task Playlists(
        [ValueSource(nameof(PlaylistSorts))] SortPlaylistBy sortBy,
        [ValueSource(nameof(Directions))] SortDirection direction)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();

        var playlists = await scope.ServiceProvider.GetRequiredService<PlaylistRepository>().GetFiltered(
            new PlaylistParameters
            {
                UserId = User.Id,
                LibraryId = _libraryId,
                Limit = 10,
                Offset = 0,
                SortBy = sortBy,
                SortDirection = direction,
            },
            transaction);

        playlists.Select(playlist => playlist.PublicId).Should().BeEquivalentTo(_playlistIds);
        if (sortBy == SortPlaylistBy.PlaylistName ||
            sortBy == SortPlaylistBy.RefreshedAt ||
            sortBy == SortPlaylistBy.SubscribedAt)
        {
            playlists.Select(playlist => playlist.PublicId).Should().Equal(Ordered(_playlistIds, direction));
        }
    }

    [Test]
    public async Task Creators(
        [ValueSource(nameof(CreatorSorts))] SortCreatorBy sortBy,
        [ValueSource(nameof(Directions))] SortDirection direction)
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();

        var act = async () => await scope.ServiceProvider.GetRequiredService<CreatorRepository>().GetFiltered(
            new CreatorParameters
            {
                UserId = User.Id,
                Limit = 10,
                Offset = 0,
                SortBy = sortBy,
                SortDirection = direction,
            },
            transaction);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task DownloadableVideos()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var act = async () => await scope.ServiceProvider.GetRequiredService<VideoRepository>().GetDownloadableVideos(
            new VideoParameters
            {
                UserId = User.Id,
                LibraryId = _libraryId,
                Limit = 10,
                Offset = 0,
                SortBy = SortVideoBy.PublishedAt,
                SortDirection = SortDirection.Descending,
            });

        await act.Should().NotThrowAsync();
    }

    private static Guid[] Ordered(Guid[] idsInAscendingOrder, SortDirection direction) =>
        direction == SortDirection.Ascending ? idsInAscendingOrder : idsInAscendingOrder.Reverse().ToArray();

    private static SortChannelBy[] ChannelSorts => SortChannelBy.List.ToArray();

    private static SortVideoBy[] VideoSorts => SortVideoBy.List.ToArray();

    private static SortPlaylistBy[] PlaylistSorts => SortPlaylistBy.List.ToArray();

    private static SortCreatorBy[] CreatorSorts => SortCreatorBy.List.ToArray();
}
