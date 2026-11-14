using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Tests.Integration.Fixtures;

[TestFixtureSource(typeof(ServerFixtureSource))]
public abstract class ServerTests
{
    protected ServerFixture Fixture { get; }

    internal TestUser User => Fixture.User;

    internal TestUser OtherUser => Fixture.OtherUser;

    protected ServerTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    internal static async Task Grant(NpgsqlConnection connection, TestUser principal, TestUser owner, Permissions permissions, NpgsqlTransaction transaction)
    {
        await connection.ExecuteAsync(
            "INSERT INTO access.grants (principal_id, owner_id, permissions, granted_by) VALUES (@PrincipalId, @OwnerId, @Permissions, @GrantedBy);",
            new { PrincipalId = principal.Id, owner.OwnerId, Permissions = (int)permissions, GrantedBy = owner.Id },
            transaction);
    }

    protected ValueTask<Guid> CreateLibrary(long userId, IServiceProvider services, NpgsqlTransaction transaction)
    {
        return CreateLibrary(string.Empty, userId, services, transaction);
    }

    protected async ValueTask<Guid> CreateLibrary(string storagePath, long userId, IServiceProvider services, NpgsqlTransaction transaction)
    {
        var ownerId = await services.GetRequiredService<OwnerRepository>().GetPersonalOwnerIdAsync(userId, transaction);
        var taskRepository = services.GetRequiredService<TaskRepository>();
        var taskId = await taskRepository.AddAsync(
            new()
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = ownerId,
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
                OwnerId = ownerId,
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
                OwnerId = ownerId,
                Name = Guid.NewGuid().ToString("N"),
                StoragePath = storagePath,
                SubscriptionsScheduleId = scheduleId!.Value,
            },
            transaction))!.Value;
    }
}
