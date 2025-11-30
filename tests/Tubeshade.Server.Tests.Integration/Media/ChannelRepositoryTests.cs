using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Media;

public sealed class ChannelRepositoryTests(ServerFixture fixture) : ServerTests(fixture)
{
    private Guid _userId;
    private Guid _libraryId;
    private Guid _libraryId2;
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

            var taskId2 = await taskRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Type = TaskType.RefreshSubscriptions,
                    UserId = _userId,
                },
                transaction);

            var scheduleId2 = await scheduleRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    TaskId = taskId2!.Value,
                    CronExpression = "* * * * *",
                    TimeZoneId = "Etc/UTC",
                },
                transaction);

            _libraryId2 = (await libraryRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    StoragePath = string.Empty,
                    SubscriptionsScheduleId = scheduleId2!.Value,
                },
                transaction))!.Value;

            await transaction.CommitAsync();
        }
    }

    [Test]
    public async Task MoveToLibrary()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();

        var primaryLibraryId = await repository.GetPrimaryLibraryId(_channelId);
        primaryLibraryId.Should().Be(_libraryId);

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var count = await repository.MoveToLibrary(_libraryId2, _channelId, _userId, transaction);
            await transaction.CommitAsync();

            count.Should().Be(1);
        }

        primaryLibraryId = await repository.GetPrimaryLibraryId(_channelId);
        primaryLibraryId.Should().Be(_libraryId2);
    }
}
