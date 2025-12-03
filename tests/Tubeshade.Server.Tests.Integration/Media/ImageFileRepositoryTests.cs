using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Media;

public sealed class ImageFileRepositoryTests(ServerFixture fixture) : ServerTests(fixture)
{
    private Guid _userId;
    private Guid _libraryId;
    private Guid _channelId;
    private Guid _videoId;

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

            var videoRepository = scope.ServiceProvider.GetRequiredService<VideoRepository>();
            _videoId = (await videoRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = string.Empty,
                    Description = string.Empty,
                    Categories = [],
                    Tags = [],
                    Type = VideoType.Video,
                    ChannelId = _channelId,
                    StoragePath = string.Empty,
                    ExternalId = Guid.NewGuid().ToString(),
                    ExternalUrl = string.Empty,
                    PublishedAt = SystemClock.Instance.GetCurrentInstant(),
                    RefreshedAt = SystemClock.Instance.GetCurrentInstant(),
                    Availability = ExternalAvailability.Public,
                    Duration = Period.Zero,
                    TotalCount = 0
                },
                transaction))!.Value;

            await transaction.CommitAsync();
        }
    }

    [Test]
    public async Task CreateReadUpdateDelete()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<ImageFileRepository>();

        Guid fileId;

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            fileId = (await repository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    StoragePath = string.Empty,
                    Type = ImageType.Thumbnail,
                    Width = 0,
                    Height = 0,
                },
                transaction))!.Value;

            await repository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    StoragePath = string.Empty,
                    Type = ImageType.Banner,
                    Width = 0,
                    Height = 0,
                },
                transaction);

            await repository.LinkToVideoAsync(fileId, _videoId, _userId, transaction);

            await transaction.CommitAsync();
        }

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var image = await repository.FindVideoThumbnail(_videoId, _userId, Access.Read, transaction);

            image.Should().NotBeNull();
            image!.Id.Should().Be(fileId);

            image.Width += 1;
            await repository.UpdateAsync(image, transaction);
            var updatedImage = await repository.FindVideoThumbnail(_videoId, _userId, Access.Read, transaction);

            updatedImage.Should().NotBeNull();
            updatedImage!.Width.Should().Be(image.Width);

            await repository.DeleteAsync(image, transaction);
            (await repository.FindVideoThumbnail(_videoId, _userId, Access.Read, transaction)).Should().BeNull();
        }
    }
}
