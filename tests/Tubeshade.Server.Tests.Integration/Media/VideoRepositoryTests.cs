using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
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
            var taskRepository = scope.ServiceProvider.GetRequiredService<TaskRepository>();
            var taskId = await taskRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Type = TaskType.RefreshSubscriptions,
                    UserId = _userId,
                },
                transaction);

            var scheduleRepository = scope.ServiceProvider.GetRequiredService<ScheduleRepository>();
            var scheduleId = await scheduleRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    TaskId = taskId!.Value,
                    CronExpression = "* * * * *",
                    TimeZoneId = "Etc/UTC",
                },
                transaction);

            var libraryRepository = scope.ServiceProvider.GetRequiredService<LibraryRepository>();
            _libraryId = (await libraryRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    StoragePath = string.Empty,
                    SubscriptionsScheduleId = scheduleId!.Value,
                },
                transaction))!.Value;

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
                UserId = _userId,
                LibraryId = _libraryId,
                Limit = 24,
                Offset = 0,
                Viewed = null,
            });

        var viewedVideos = await repository.GetFiltered(
            new VideoParameters
            {
                UserId = _userId,
                LibraryId = _libraryId,
                Limit = 24,
                Offset = 0,
                Viewed = true,
            });

        var notViewedVideos = await repository.GetFiltered(
            new VideoParameters
            {
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
}
