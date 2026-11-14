using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Media;

public sealed class ChannelRepositoryTests(ServerFixture fixture) : ServerTests(fixture)
{
    private Guid _libraryId;
    private Guid _libraryId2;
    private Guid _libraryId3;
    private Guid _channelId;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();


        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            _libraryId = await CreateLibrary(User.Id, scope.ServiceProvider, transaction);
            _libraryId2 = await CreateLibrary(User.Id, scope.ServiceProvider, transaction);
            _libraryId3 = await CreateLibrary(Guid.NewGuid().ToString(), User.Id, scope.ServiceProvider, transaction);

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
    public async Task MoveToLibrary()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();

        (await repository.GetPrimaryLibraryId(_channelId, User.Id)).Should().Be(_libraryId);

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var count = await repository.MoveToLibrary(_libraryId2, _channelId, User.Id, transaction);
            await transaction.CommitAsync();

            count.Should().Be(1);
        }

        (await repository.GetPrimaryLibraryId(_channelId, User.Id)).Should().Be(_libraryId2);

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var count = await repository.MoveToLibrary(_libraryId, _channelId, User.Id, transaction);
            await transaction.CommitAsync();

            count.Should().Be(1);
        }

        (await repository.GetPrimaryLibraryId(_channelId, User.Id)).Should().Be(_libraryId);
    }

    [Test]
    public async Task MoveOnlyIfStoragePathMatches()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();

        var currentId = await repository.GetPrimaryLibraryId(_channelId, User.Id);

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var count = await repository.MoveToLibrary(_libraryId3, _channelId, User.Id, transaction);
            await transaction.CommitAsync();

            count.Should().Be(0);
        }

        (await repository.GetPrimaryLibraryId(_channelId, User.Id)).Should().Be(currentId);
    }

    [Test]
    public async Task MoveToLibraryOfAnotherOwner()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();
        await using var transaction = await connection.OpenAndBeginTransaction();

        var otherLibraryId = await CreateLibrary(OtherUser.Id, scope.ServiceProvider, transaction);
        await Grant(connection, User, OtherUser, Permissions.Read | Permissions.Update, transaction);
        var (channelId, videoId, fileId) = await CreateChannelWithFile(scope.ServiceProvider, User, transaction);

        (await repository.MoveToLibrary(otherLibraryId, channelId, User.Id, transaction)).Should().Be(1);

        var owners = await connection.QuerySingleAsync<long[]>(
            """
            SELECT ARRAY[(SELECT owner_id FROM media.channels WHERE public_id = @channelId),
                         (SELECT owner_id FROM media.videos WHERE public_id = @videoId),
                         (SELECT owner_id FROM media.video_files WHERE public_id = @fileId)];
            """,
            new { channelId, videoId, fileId },
            transaction);

        owners.Should().Equal(OtherUser.OwnerId, OtherUser.OwnerId, OtherUser.OwnerId);

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task MoveToAnotherOwnerNeedsDeleteOnTheChannel()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();
        await using var transaction = await connection.OpenAndBeginTransaction();

        var otherLibraryId = await CreateLibrary(OtherUser.Id, scope.ServiceProvider, transaction);
        await Grant(connection, OtherUser, User, Permissions.Read | Permissions.Update, transaction);
        var (channelId, _, _) = await CreateChannelWithFile(scope.ServiceProvider, User, transaction);

        (await repository.MoveToLibrary(otherLibraryId, channelId, OtherUser.Id, transaction)).Should().Be(0);
        (await repository.GetPrimaryLibraryId(channelId, User.Id, transaction)).Should().Be(_libraryId);

        await transaction.RollbackAsync();
    }

    private async Task<(Guid ChannelId, Guid VideoId, Guid FileId)> CreateChannelWithFile(
        IServiceProvider services,
        TestUser user,
        NpgsqlTransaction transaction)
    {
        var channelRepository = services.GetRequiredService<ChannelRepository>();
        var channelId = (await channelRepository.AddAsync(
            new()
            {
                CreatedByUserId = user.Id,
                ModifiedByUserId = user.Id,
                OwnerId = user.OwnerId,
                Name = Guid.NewGuid().ToString("N"),
                StoragePath = string.Empty,
                ExternalId = string.Empty,
                ExternalUrl = string.Empty,
                Availability = ExternalAvailability.Public,
            },
            transaction))!.Value;

        await channelRepository.AddToLibrary(_libraryId, channelId, user.Id, transaction);

        var now = SystemClock.Instance.GetCurrentInstant();
        var videoId = (await services.GetRequiredService<VideoRepository>().AddAsync(
            new()
            {
                CreatedByUserId = user.Id,
                ModifiedByUserId = user.Id,
                OwnerId = user.OwnerId,
                Name = Guid.NewGuid().ToString("N"),
                Description = string.Empty,
                Categories = [],
                Tags = [],
                Type = VideoType.Video,
                ChannelId = channelId,
                StoragePath = string.Empty,
                ExternalId = string.Empty,
                ExternalUrl = "https://example.org",
                PublishedAt = now,
                RefreshedAt = now,
                Availability = ExternalAvailability.Public,
                Duration = Period.FromMinutes(10),
            },
            transaction))!.Value;

        var fileId = (await services.GetRequiredService<VideoFileRepository>().AddAsync(
            new VideoFileEntity
            {
                CreatedByUserId = user.Id,
                ModifiedByUserId = user.Id,
                OwnerId = user.OwnerId,
                VideoId = videoId,
                StoragePath = string.Empty,
                Type = VideoContainerType.Mp4,
                Width = 1920,
                Height = 1080,
                Framerate = 30,
            },
            transaction))!.Value;

        return (channelId, videoId, fileId);
    }
}
