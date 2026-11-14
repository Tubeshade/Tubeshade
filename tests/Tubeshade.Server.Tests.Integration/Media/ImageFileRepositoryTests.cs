using System;
using System.IO;
using System.Threading.Tasks;
using AwesomeAssertions;
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

public sealed class ImageFileRepositoryTests(ServerFixture fixture) : ServerTests(fixture)
{
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

            var videoRepository = scope.ServiceProvider.GetRequiredService<VideoRepository>();
            _videoId = (await videoRepository.AddAsync(
                new()
                {
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
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
                    Duration = Period.FromMinutes(10),
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
            var hashAlgorithm = HashAlgorithm.Default;

            using var content1 = new MemoryStream(Guid.NewGuid().ToByteArray());
            using var content2 = new MemoryStream(Guid.NewGuid().ToByteArray());

            fileId = (await repository.AddAsync(
                new()
                {
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    StoragePath = string.Empty,
                    Type = ImageType.Thumbnail,
                    Width = 0,
                    Height = 0,
                    HashAlgorithm = hashAlgorithm,
                    Hash = await hashAlgorithm.ComputeHashAsync(content1),
                    StorageSize = content1.Length,
                },
                transaction))!.Value;

            await repository.AddAsync(
                new()
                {
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    StoragePath = string.Empty,
                    Type = ImageType.Banner,
                    Width = 0,
                    Height = 0,
                    HashAlgorithm = hashAlgorithm,
                    Hash = await hashAlgorithm.ComputeHashAsync(content2),
                    StorageSize = content2.Length,
                },
                transaction);

            await repository.LinkToVideoAsync(fileId, _videoId, User.Id, transaction);

            await transaction.CommitAsync();
        }

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var image = (await repository.GetForVideo(_videoId, User.Id, Permissions.Read, transaction))
                .Should()
                .ContainSingle()
                .Subject;

            image.PublicId.Should().Be(fileId);

            image.Width += 1;
            await repository.UpdateAsync(image, transaction);
            (await repository.GetForVideo(_videoId, User.Id, Permissions.Read, transaction))
                .Should()
                .ContainSingle()
                .Which.Width.Should()
                .Be(image.Width);

            await repository.DeleteAsync(image, transaction);
            (await repository.GetForVideo(_videoId, User.Id, Permissions.Read, transaction)).Should().BeEmpty();
        }
    }

    [Test]
    public async Task DeleteChannelImage()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<ImageFileRepository>();

        await using var transaction = await connection.OpenAndBeginTransaction();

        var hashAlgorithm = HashAlgorithm.Default;
        using var content = new MemoryStream(Guid.NewGuid().ToByteArray());

        var fileId = (await repository.AddAsync(
            new()
            {
                CreatedByUserId = User.Id,
                ModifiedByUserId = User.Id,
                StoragePath = string.Empty,
                Type = ImageType.Thumbnail,
                Width = 1,
                Height = 1,
                HashAlgorithm = hashAlgorithm,
                Hash = await hashAlgorithm.ComputeHashAsync(content),
                StorageSize = content.Length,
            },
            transaction))!.Value;

        await repository.LinkToChannelAsync(fileId, _channelId, User.Id, transaction);
        (await repository.GetForChannel(_channelId, User.Id, Permissions.Read, transaction)).Should().ContainSingle();

        (await repository.DeleteAsync(fileId, User.Id, transaction)).Should().Be(1);
        (await repository.GetForChannel(_channelId, User.Id, Permissions.Read, transaction)).Should().BeEmpty();
    }
}
