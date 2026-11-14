using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
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

/// <summary>Videos take the owner of their channel and files the owner of their video, whatever the library or the caller says.</summary>
public sealed class OwnershipTests(ServerFixture fixture) : ServerTests(fixture)
{
    [Test]
    public async Task VideosAndFilesFollowTheChannelInAnotherOwnersLibrary()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();
        var videoRepository = scope.ServiceProvider.GetRequiredService<VideoRepository>();
        var fileRepository = scope.ServiceProvider.GetRequiredService<VideoFileRepository>();

        var libraryId = await CreateLibrary(User.Id, scope.ServiceProvider, transaction);
        await Grant(connection, User, OtherUser, Permissions.Read | Permissions.Create | Permissions.Update | Permissions.Delete, transaction);
        var channelId = await CreateChannel(scope.ServiceProvider, OtherUser.OwnerId, libraryId, transaction);

        var videoId = await CreateVideo(scope.ServiceProvider, channelId, transaction);
        var fileId = (await fileRepository.AddAsync(File(videoId), transaction))!.Value;

        var owners = await connection.QuerySingleAsync<long[]>(
            """
            SELECT ARRAY[(SELECT owner_id FROM media.libraries WHERE public_id = @libraryId),
                         (SELECT owner_id FROM media.videos WHERE public_id = @videoId),
                         (SELECT owner_id FROM media.video_files WHERE public_id = @fileId)];
            """,
            new { libraryId, videoId, fileId },
            transaction);

        using (new AssertionScope())
        {
            owners.Should().Equal(User.OwnerId, OtherUser.OwnerId, OtherUser.OwnerId);
            (await videoRepository.GetFilesAsync(videoId, User.Id, transaction)).Should().ContainSingle().Which.PublicId.Should().Be(fileId);
            (await fileRepository.DeleteAsync(fileId, User.Id, transaction)).Should().Be(1);
        }

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task AddingAFileNeedsCreateOnTheVideosOwner()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();
        var fileRepository = scope.ServiceProvider.GetRequiredService<VideoFileRepository>();

        var libraryId = await CreateLibrary(User.Id, scope.ServiceProvider, transaction);
        await Grant(connection, User, OtherUser, Permissions.Read | Permissions.Create | Permissions.Update, transaction);
        var channelId = await CreateChannel(scope.ServiceProvider, OtherUser.OwnerId, libraryId, transaction);
        var videoId = await CreateVideo(scope.ServiceProvider, channelId, transaction);

        // Only Read on the video's owner, while the user can still create in their own owner, which a caller could have chosen
        await connection.ExecuteAsync(
            "UPDATE access.grants SET permissions = @Read WHERE principal_id = @PrincipalId AND owner_id = @OwnerId;",
            new { Read = (int)Permissions.Read, PrincipalId = User.Id, OtherUser.OwnerId },
            transaction);

        (await fileRepository.AddAsync(File(videoId), transaction)).Should().BeNull();

        await transaction.RollbackAsync();
    }

    private async Task<Guid> CreateChannel(IServiceProvider services, long ownerId, Guid libraryId, NpgsqlTransaction transaction)
    {
        var channelRepository = services.GetRequiredService<ChannelRepository>();
        var channelId = (await channelRepository.AddAsync(
            new()
            {
                CreatedByUserId = User.Id,
                ModifiedByUserId = User.Id,
                OwnerId = ownerId,
                Name = Guid.NewGuid().ToString("N"),
                StoragePath = string.Empty,
                ExternalId = string.Empty,
                ExternalUrl = string.Empty,
                Availability = ExternalAvailability.Public,
            },
            transaction))!.Value;

        (await channelRepository.AddToLibrary(libraryId, channelId, User.Id, transaction)).Should().Be(1);
        return channelId;
    }

    private async Task<Guid> CreateVideo(IServiceProvider services, Guid channelId, NpgsqlTransaction transaction)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        return (await services.GetRequiredService<VideoRepository>().AddAsync(
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
    }

    private VideoFileEntity File(Guid videoId) => new()
    {
        CreatedByUserId = User.Id,
        ModifiedByUserId = User.Id,
        OwnerId = User.OwnerId,
        VideoId = videoId,
        StoragePath = string.Empty,
        Type = VideoContainerType.Mp4,
        Width = 1920,
        Height = 1080,
        Framerate = 30,
    };
}
