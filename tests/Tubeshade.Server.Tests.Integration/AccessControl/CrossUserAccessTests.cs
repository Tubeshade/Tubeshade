using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Npgsql;
using NUnit.Framework;
using SponsorBlock;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Creators;
using Tubeshade.Data.Media.Playlists;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Data.Preferences;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.AccessControl;

/// <summary>
/// For each repository: a user without a grant cannot read, update or delete another user's data,
/// a user with only Read cannot change it, and the owner and the system paths still can.
/// </summary>
public sealed class CrossUserAccessTests(ServerFixture fixture) : ServerTests(fixture)
{
    private static readonly SystemPrincipal System = new("Testing system access");

    private TestUser _owner = null!;
    private TestUser _stranger = null!;
    private TestUser _reader = null!;

    private Guid _libraryId;
    private Guid _channelId;
    private Guid _videoId;
    private Guid _videoFileId;
    private Guid _imageId;
    private Guid _trackId;
    private Guid _playlistId;
    private Guid _creatorId;
    private Guid _segmentId;
    private Guid _preferencesId;
    private Guid _taskId;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _owner = await ServerFixture.CreateUserAsync(Fixture.Services, $"owner-{Guid.NewGuid():N}");
        _stranger = await ServerFixture.CreateUserAsync(Fixture.Services, $"stranger-{Guid.NewGuid():N}");
        _reader = await ServerFixture.CreateUserAsync(Fixture.Services, $"reader-{Guid.NewGuid():N}");

        await using var scope = Fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var connection = services.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();

        await connection.ExecuteAsync(
            """
            INSERT INTO access.grants (principal_id, owner_id, permissions, granted_by)
            VALUES (@PrincipalId, @OwnerId, @Permissions, @OwnerUserId);
            """,
            new { PrincipalId = _reader.Id, _owner.OwnerId, Permissions = (int)Permissions.Read, OwnerUserId = _owner.Id },
            transaction);

        var userId = _owner.Id;
        var now = SystemClock.Instance.GetCurrentInstant();

        _libraryId = await CreateLibrary(Guid.NewGuid().ToString("N"), userId, services, transaction);

        var channelRepository = services.GetRequiredService<ChannelRepository>();
        _channelId = (await channelRepository.AddAsync(
            new ChannelEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = _owner.OwnerId,
                Name = "channel",
                StoragePath = string.Empty,
                ExternalId = Guid.NewGuid().ToString("N"),
                ExternalUrl = string.Empty,
                Availability = ExternalAvailability.Public,
            },
            transaction))!.Value;
        (await channelRepository.AddToLibrary(_libraryId, _channelId, userId, transaction)).Should().Be(1);

        _videoId = (await services.GetRequiredService<VideoRepository>().AddAsync(
            new VideoEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = _owner.OwnerId,
                Name = "video",
                Description = string.Empty,
                Categories = [],
                Tags = [],
                Type = VideoType.Video,
                ChannelId = _channelId,
                StoragePath = string.Empty,
                ExternalId = Guid.NewGuid().ToString("N"),
                ExternalUrl = $"https://example.org/{Guid.NewGuid():N}",
                PublishedAt = now,
                RefreshedAt = now,
                Availability = ExternalAvailability.Public,
                Duration = Period.FromMinutes(10),
            },
            transaction))!.Value;

        _videoFileId = (await services.GetRequiredService<VideoFileRepository>().AddAsync(
            new VideoFileEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = _owner.OwnerId,
                VideoId = _videoId,
                StoragePath = string.Empty,
                Type = VideoContainerType.Mp4,
                Width = 1920,
                Height = 1080,
                Framerate = 30,
                DownloadedAt = now,
                DownloadedByUserId = userId,
            },
            transaction))!.Value;

        var imageRepository = services.GetRequiredService<ImageFileRepository>();
        _imageId = (await imageRepository.AddAsync(
            new ImageFileEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                StoragePath = string.Empty,
                Type = ImageType.Thumbnail,
                Width = 1,
                Height = 1,
                Hash = [],
                HashAlgorithm = HashAlgorithm.Placeholder,
                StorageSize = 0,
            },
            transaction))!.Value;
        (await imageRepository.LinkToVideoAsync(_imageId, _videoId, userId, transaction)).Should().Be(1);

        _trackId = (await services.GetRequiredService<TrackFileRepository>().AddAsync(
            new TrackFileEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                VideoId = _videoId,
                StoragePath = string.Empty,
                Type = TrackType.Subtitles,
                Language = "en",
                Hash = [],
                HashAlgorithm = HashAlgorithm.Placeholder,
                StorageSize = 0,
            },
            transaction))!.Value;

        _playlistId = (await services.GetRequiredService<PlaylistRepository>().AddAsync(
            new PlaylistEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                LibraryId = _libraryId,
                StoragePath = string.Empty,
                ExternalId = Guid.NewGuid().ToString("N"),
                ExternalUrl = string.Empty,
                Name = "playlist",
                RefreshedAt = now,
            },
            transaction))!.Value;

        _creatorId = (await services.GetRequiredService<CreatorRepository>().AddAsync(
            new CreatorEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = _owner.OwnerId,
                Name = "creator",
            },
            transaction))!.Value;

        _segmentId = (await services.GetRequiredService<SponsorBlockSegmentRepository>().AddAsync(
            new SponsorBlockSegmentEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                VideoId = _videoId,
                ExternalId = Guid.NewGuid().ToString("N"),
                StartTime = 1,
                EndTime = 2,
                Category = SegmentCategory.Sponsor,
                Action = SegmentAction.Skip,
                Locked = false,
            },
            transaction))!.Value;

        var preferencesRepository = services.GetRequiredService<PreferencesRepository>();
        _preferencesId = (await preferencesRepository.AddAsync(
            new PreferencesEntity { CreatedByUserId = userId, ModifiedByUserId = userId, VideosCount = 1 },
            transaction))!.Value;
        (await preferencesRepository.LinkToLibrary(_preferencesId, _libraryId, userId, transaction)).Should().Be(1);

        (await services.GetRequiredService<LibraryCookieRepository>().AddAsync(
            new LibraryCookieEntity
            {
                PublicId = _libraryId,
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                Domain = "example.org",
                Cookie = "cookie",
            },
            transaction)).Should().Be(_libraryId);

        (await services.GetRequiredService<ChannelSubscriptionRepository>().AddAsync(
            new ChannelSubscriptionEntity
            {
                PublicId = _channelId,
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                Status = SubscriptionStatus.SubscriptionPending,
                Callback = "https://example.org/callback",
                Topic = "https://example.org/topic",
            },
            transaction)).Should().Be(_channelId);

        _taskId = await services.GetRequiredService<TaskRepository>()
            .AddTask(TaskEntity.ScanChannel(_libraryId, userId, _channelId, false), transaction);

        await transaction.CommitAsync();
    }

    [Test]
    public async Task Libraries()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<LibraryRepository>();

        (await repository.FindAsync(_libraryId, _owner.Id, Permissions.Read, transaction)).Should().NotBeNull();
        (await repository.FindAsync(_libraryId, _stranger.Id, Permissions.Read, transaction)).Should().BeNull();
        (await repository.GetAsync(_stranger.Id, transaction)).Should().NotContain(library => library.PublicId == _libraryId);
        (await repository.FindAsync(_libraryId, _reader.Id, Permissions.Read, transaction)).Should().NotBeNull();
        (await repository.FindAsync(_libraryId, _reader.Id, Permissions.Update, transaction)).Should().BeNull();
        (await repository.FindForSystemAsync(System, _libraryId, transaction)).Should().NotBeNull();

        var library = (await repository.FindAsync(_libraryId, _owner.Id, Permissions.Read, transaction))!;
        (await repository.UpdateAsync(library with { ModifiedByUserId = _stranger.Id, Name = "changed" }, transaction)).Should().Be(0);
        (await repository.UpdateAsync(library with { ModifiedByUserId = _reader.Id, Name = "changed" }, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_libraryId, _stranger.Id, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_libraryId, _reader.Id, transaction)).Should().Be(0);
        (await repository.UpdateAsync(library with { Name = "changed" }, transaction)).Should().Be(1);

        (await repository.AddAsync(
                library with { CreatedByUserId = _reader.Id, ModifiedByUserId = _reader.Id, StoragePath = Guid.NewGuid().ToString() },
                transaction))
            .Should().BeNull();

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task Channels()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();

        (await repository.FindAsync(_channelId, _stranger.Id, Permissions.Read, transaction)).Should().BeNull();
        (await repository.GetForLibrary(_libraryId, _stranger.Id)).Should().BeEmpty();
        (await repository.FindAsync(_channelId, _reader.Id, Permissions.Read, transaction)).Should().NotBeNull();
        (await repository.FindForSystemAsync(System, _channelId, transaction)).Should().NotBeNull();

        var channel = (await repository.FindAsync(_channelId, _owner.Id, Permissions.Read, transaction))!;
        (await repository.UpdateAsync(channel with { ModifiedByUserId = _stranger.Id, Name = "changed" }, transaction)).Should().Be(0);
        (await repository.UpdateAsync(channel with { ModifiedByUserId = _reader.Id, Name = "changed" }, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_channelId, _stranger.Id, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_channelId, _reader.Id, transaction)).Should().Be(0);

        (await repository.AddAsync(
                channel with { CreatedByUserId = _stranger.Id, ModifiedByUserId = _stranger.Id, ExternalId = Guid.NewGuid().ToString("N") },
                transaction))
            .Should().BeNull();

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task Videos()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<VideoRepository>();

        (await repository.FindAsync(_videoId, _stranger.Id, Permissions.Read, transaction)).Should().BeNull();
        (await repository.GetFilesAsync(_videoId, _stranger.Id, transaction)).Should().BeEmpty();
        (await repository.FindFileAsync(_videoFileId, _stranger.Id)).Should().BeNull();
        (await repository.GetFiltered(Parameters(_stranger.Id))).Should().BeEmpty();
        (await repository.SetWatched(_videoId, _stranger.Id, true, transaction)).Should().Be(0);
        (await repository.AddPlay(_videoId, _stranger.Id, transaction)).Should().Be(0);

        (await repository.FindAsync(_videoId, _reader.Id, Permissions.Read, transaction)).Should().NotBeNull();
        (await repository.GetFiltered(Parameters(_reader.Id))).Should().ContainSingle();
        (await repository.FindAsync(_videoId, _reader.Id, Permissions.Update, transaction)).Should().BeNull();

        var video = (await repository.FindAsync(_videoId, _owner.Id, Permissions.Read, transaction))!;
        (await repository.UpdateAsync(video with { ModifiedByUserId = _stranger.Id, Name = "changed" }, transaction)).Should().Be(0);
        (await repository.UpdateAsync(video with { ModifiedByUserId = _reader.Id, Name = "changed" }, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_videoId, _reader.Id, transaction)).Should().Be(0);
        (await repository.UpdateAsync(video with { Name = "changed" }, transaction)).Should().Be(1);

        var fileRepository = scope.ServiceProvider.GetRequiredService<VideoFileRepository>();
        (await fileRepository.CreateTemporaryFile(_videoFileId, _stranger.Id, Guid.NewGuid(), "path", CancellationToken.None)).Should().BeFalse();
        (await fileRepository.FindAsync(_videoFileId, _stranger.Id, Permissions.Read, transaction)).Should().BeNull();
        (await fileRepository.DeleteAsync(_videoFileId, _reader.Id, transaction)).Should().Be(0);

        await transaction.RollbackAsync();

        VideoParameters Parameters(long userId) => new()
        {
            UserId = userId,
            ChannelId = _channelId,
            Limit = 10,
            Offset = 0,
            SortBy = SortVideoBy.PublishedAt,
            SortDirection = SortDirection.Descending,
        };
    }

    [Test]
    public async Task Images()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<ImageFileRepository>();

        (await repository.FindAsync(_imageId, _stranger.Id, Permissions.Read, transaction)).Should().BeNull();
        (await repository.GetForVideo(_videoId, _stranger.Id, Permissions.Read, transaction)).Should().BeEmpty();
        (await repository.GetPath(_imageId, _stranger.Id, transaction, CancellationToken.None)).Should().BeNull();
        (await repository.LinkToChannelAsync(_imageId, _channelId, _stranger.Id, transaction)).Should().Be(0);
        (await repository.LinkToChannelAsync(_imageId, _channelId, _reader.Id, transaction)).Should().Be(0);

        (await repository.FindAsync(_imageId, _reader.Id, Permissions.Read, transaction)).Should().NotBeNull();
        (await repository.GetPath(_imageId, _reader.Id, transaction, CancellationToken.None)).Should().NotBeNull();

        var image = (await repository.FindAsync(_imageId, _owner.Id, Permissions.Read, transaction))!;
        (await repository.UpdateAsync(image with { ModifiedByUserId = _reader.Id, Width = 2 }, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_imageId, _stranger.Id, transaction)).Should().Be(0);
        (await repository.UpdateAsync(image with { Width = 2 }, transaction)).Should().Be(1);

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task Tracks()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<TrackFileRepository>();

        (await repository.GetForVideo(_videoId, _stranger.Id, Permissions.Read, transaction)).Should().BeEmpty();
        (await repository.FindAsync(_trackId, _stranger.Id, Permissions.Read, transaction)).Should().BeNull();
        (await repository.GetForVideo(_videoId, _reader.Id, Permissions.Read, transaction)).Should().ContainSingle();

        var track = (await repository.FindAsync(_trackId, _owner.Id, Permissions.Read, transaction))!;
        (await repository.UpdateAsync(track with { ModifiedByUserId = _reader.Id, StorageSize = 1 }, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_trackId, _reader.Id, transaction)).Should().Be(0);
        (await repository.UpdateAsync(track with { StorageSize = 1 }, transaction)).Should().Be(1);

        (await repository.AddAsync(track with { CreatedByUserId = _stranger.Id }, transaction)).Should().BeNull();

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task Playlists()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<PlaylistRepository>();

        (await repository.FindAsync(_playlistId, _stranger.Id, Permissions.Read, transaction)).Should().BeNull();
        (await repository.GetVideoUrls(_playlistId, _stranger.Id, transaction)).Should().BeEmpty();
        (await repository.ReplaceVideos(_playlistId, [_videoId], _stranger.Id, transaction)).Should().BeFalse();
        (await repository.ReplaceVideos(_playlistId, [_videoId], _reader.Id, transaction)).Should().BeFalse();
        (await repository.FindAsync(_playlistId, _reader.Id, Permissions.Read, transaction)).Should().NotBeNull();

        var playlist = (await repository.FindAsync(_playlistId, _owner.Id, Permissions.Read, transaction))!;
        (await repository.UpdateAsync(playlist with { ModifiedByUserId = _reader.Id, Name = "changed" }, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_playlistId, _stranger.Id, transaction)).Should().Be(0);
        (await repository.ReplaceVideos(_playlistId, [_videoId], _owner.Id, transaction)).Should().BeTrue();
        (await repository.GetVideoUrls(_playlistId, _owner.Id, transaction)).Should().ContainSingle();

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task PlaylistVideosReplacedAgain()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<PlaylistRepository>();

        (await repository.ReplaceVideos(_playlistId, [_videoId, _videoId], _owner.Id, transaction)).Should().BeTrue();
        (await repository.ReplaceVideos(_playlistId, [_videoId, _videoId, _videoId], _owner.Id, transaction)).Should().BeTrue();
        (await repository.GetVideoUrls(_playlistId, _owner.Id, transaction)).Should().HaveCount(3);

        (await repository.ReplaceVideos(_playlistId, [_videoId], _owner.Id, transaction)).Should().BeTrue();
        (await repository.GetVideoUrls(_playlistId, _owner.Id, transaction)).Should().ContainSingle();

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task Creators()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<CreatorRepository>();

        (await repository.FindAsync(_creatorId, _stranger.Id, Permissions.Read, transaction)).Should().BeNull();
        await repository.UpdateChannels(_creatorId, _channelId, [], _stranger.Id, transaction);
        await repository.UpdateChannels(_creatorId, _channelId, [], _reader.Id, transaction);
        (await repository.GetChannels(_creatorId, _owner.Id)).Should().BeEmpty();

        var creator = (await repository.FindAsync(_creatorId, _owner.Id, Permissions.Read, transaction))!;
        (await repository.UpdateAsync(creator with { ModifiedByUserId = _reader.Id, Name = "changed" }, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_creatorId, _reader.Id, transaction)).Should().Be(0);

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task SponsorBlockSegments()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<SponsorBlockSegmentRepository>();

        (await repository.GetForVideo(_videoId, _stranger.Id, transaction)).Should().BeEmpty();
        (await repository.GetForVideos([_videoId], _stranger.Id)).Should().BeEmpty();
        (await repository.GetForVideo(_videoId, _reader.Id, transaction)).Should().ContainSingle();

        var segment = (await repository.GetForVideo(_videoId, _owner.Id, transaction)).Single();
        (await repository.UpdateAsync(segment with { ModifiedByUserId = _stranger.Id, Locked = true }, transaction)).Should().Be(0);
        (await repository.UpdateAsync(segment with { ModifiedByUserId = _reader.Id, Locked = true }, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_segmentId, _stranger.Id, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_segmentId, _reader.Id, transaction)).Should().Be(0);
        (await repository.UpdateAsync(segment with { Locked = true }, transaction)).Should().Be(1);
        (await repository.DeleteAsync(_segmentId, _owner.Id, transaction)).Should().Be(1);

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task Preferences()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<PreferencesRepository>();

        (await repository.FindForLibrary(_libraryId, _stranger.Id, transaction)).Should().BeNull();
        (await repository.GetEffectiveForLibrary(_libraryId, _stranger.Id, CancellationToken.None)).Should().BeNull();
        (await repository.GetEffectiveForChannel(_libraryId, _channelId, _stranger.Id, transaction, CancellationToken.None)).Should().BeNull();
        (await repository.GetEffectiveForVideo(_libraryId, _videoId, _stranger.Id, transaction, CancellationToken.None)).Should().BeNull();
        (await repository.FindAsync(_preferencesId, _stranger.Id, Permissions.Read, transaction)).Should().BeNull();
        (await repository.LinkToChannel(_preferencesId, _channelId, _stranger.Id, transaction)).Should().Be(0);
        (await repository.LinkToChannel(_preferencesId, _channelId, _reader.Id, transaction)).Should().Be(0);

        (await repository.FindForLibrary(_libraryId, _reader.Id, transaction)).Should().NotBeNull();

        var preferences = (await repository.FindForLibrary(_libraryId, _owner.Id, transaction))!;
        (await repository.UpdateAsync(preferences with { ModifiedByUserId = _stranger.Id, VideosCount = 2 }, transaction)).Should().Be(0);
        (await repository.UpdateAsync(preferences with { ModifiedByUserId = _reader.Id, VideosCount = 2 }, transaction)).Should().Be(0);
        (await repository.UpdateAsync(preferences with { VideosCount = 2 }, transaction)).Should().Be(1);

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task Cookies()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<LibraryCookieRepository>();

        (await repository.FindByDomain("example.org", _stranger.Id, _libraryId, Permissions.Read, transaction)).Should().BeNull();
        (await repository.FindByDomain("example.org", _reader.Id, _libraryId, Permissions.Read, transaction)).Should().NotBeNull();

        var cookie = (await repository.FindByDomain("example.org", _owner.Id, _libraryId, Permissions.Read, transaction))!;
        (await repository.UpdateAsync(cookie with { ModifiedByUserId = _stranger.Id, Cookie = "changed" }, transaction)).Should().Be(0);
        (await repository.UpdateAsync(cookie with { ModifiedByUserId = _reader.Id, Cookie = "changed" }, transaction)).Should().Be(0);
        (await repository.UpdateAsync(cookie with { Cookie = "changed" }, transaction)).Should().Be(1);

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task ChannelSubscriptions()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<ChannelSubscriptionRepository>();

        (await repository.FindAsync(_channelId, _stranger.Id, Permissions.Read, transaction)).Should().BeNull();
        (await repository.FindAsync(_channelId, _reader.Id, Permissions.Read, transaction)).Should().NotBeNull();

        var subscription = (await repository.FindForSystemAsync(System, _channelId, transaction))!;
        var subscribed = subscription with { Status = SubscriptionStatus.Subscribed };
        (await repository.UpdateAsync(subscribed with { ModifiedByUserId = _stranger.Id }, transaction)).Should().Be(0);
        (await repository.UpdateAsync(subscribed with { ModifiedByUserId = _reader.Id }, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_channelId, _reader.Id, transaction)).Should().Be(0);
        (await repository.UpdateForSystemAsync(System, subscribed, transaction)).Should().Be(1);
        (await repository.UpdateAsync(subscribed with { ModifiedByUserId = _owner.Id }, transaction)).Should().Be(1);
        (await repository.DeleteForSystemAsync(System, subscribed.Id, transaction)).Should().Be(1);

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task Tasks()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (_, transaction) = await Begin(scope);
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        await repository.TriggerTask(_taskId, TaskSource.User, _owner.Id, transaction);
        await repository.AddTaskRunForSystemAsync(System, Guid.NewGuid(), _taskId, TaskSource.User, transaction);

        (await repository.FindAsync(_taskId, _stranger.Id, Permissions.Read, transaction)).Should().BeNull();
        (await repository.GetRunningTasks(Parameters(_stranger.Id), transaction, CancellationToken.None)).Should().BeEmpty();
        (await repository.GetRunningTasks(Parameters(_reader.Id), transaction, CancellationToken.None)).Should().NotBeEmpty();
        (await repository.GetRunningTasks(Parameters(_owner.Id), transaction, CancellationToken.None)).Should().NotBeEmpty();

        var triggerByStranger = async () => await repository.TriggerTask(_taskId, TaskSource.User, _stranger.Id, transaction);
        await triggerByStranger.Should().ThrowAsync<InvalidOperationException>();

        var task = (await repository.FindAsync(_taskId, _owner.Id, Permissions.Read, transaction))!;
        (await repository.UpdateAsync(task with { ModifiedByUserId = _reader.Id, Url = "changed" }, transaction)).Should().Be(0);
        (await repository.DeleteAsync(_taskId, _reader.Id, transaction)).Should().Be(0);

        (await repository.TryAddTask(TaskEntity.ScanChannel(_libraryId, _stranger.Id, _channelId, true), transaction)).Should().BeNull();

        var scheduleRepository = scope.ServiceProvider.GetRequiredService<ScheduleRepository>();
        (await scheduleRepository.GetForSystemAsync(System, CancellationToken.None)).Should().NotBeEmpty();

        await transaction.RollbackAsync();

        TaskParameters Parameters(long userId) => new()
        {
            UserId = userId,
            LibraryId = _libraryId,
            Limit = 10,
            Offset = 0,
        };
    }

    [Test]
    public async Task SystemOwnerIsReadableByEveryUser()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var (connection, transaction) = await Begin(scope);

        var permissions = await connection.QueryAsync<int>(
            """
            SELECT grants.permissions
            FROM access.grants
                INNER JOIN access.owners ON owners.id = grants.owner_id
                INNER JOIN identity.users ON users.id = owners.personal_for
            WHERE users.normalized_name = 'SYSTEM' AND grants.principal_id = @Id;
            """,
            new { _stranger.Id },
            transaction);

        permissions.Should().Equal((int)Permissions.Read);

        var ownPermissions = await connection.QuerySingleAsync<int>(
            "SELECT permissions FROM access.grants WHERE principal_id = @Id AND owner_id = @OwnerId;",
            new { _stranger.Id, _stranger.OwnerId },
            transaction);

        ownPermissions.Should().Be((int)Permissions.All);

        await transaction.RollbackAsync();
    }

    private static async Task<(NpgsqlConnection Connection, NpgsqlTransaction Transaction)> Begin(AsyncServiceScope scope)
    {
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var transaction = await connection.OpenAndBeginTransaction();
        return (connection, transaction);
    }
}
