using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Tests.Integration.Fixtures;

[TestFixtureSource(typeof(ServerFixtureSource))]
public abstract class ServerTests
{
    protected ServerFixture Fixture { get; }

    protected ServerTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    protected ValueTask<Guid> CreateLibrary(Guid userId, IServiceProvider services, NpgsqlTransaction transaction)
    {
        return CreateLibrary(string.Empty, userId, services, transaction);
    }

    protected async ValueTask<Guid> CreateLibrary(string storagePath, Guid userId, IServiceProvider services, NpgsqlTransaction transaction)
    {
        var taskRepository = services.GetRequiredService<TaskRepository>();
        var taskId = await taskRepository.AddAsync(
            new()
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Type = TaskType.RefreshSubscriptions,
                UserId = userId,
            },
            transaction);

        var scheduleRepository = services.GetRequiredService<ScheduleRepository>();
        var scheduleId = await scheduleRepository.AddAsync(
            new()
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                TaskId = taskId!.Value,
                CronExpression = "* * * * *",
                TimeZoneId = "Etc/UTC",
            },
            transaction);

        var libraryRepository = services.GetRequiredService<LibraryRepository>();
        return (await libraryRepository.AddAsync(
            new()
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Name = Guid.NewGuid().ToString("N"),
                StoragePath = storagePath,
                SubscriptionsScheduleId = scheduleId!.Value,
            },
            transaction))!.Value;
    }
}
