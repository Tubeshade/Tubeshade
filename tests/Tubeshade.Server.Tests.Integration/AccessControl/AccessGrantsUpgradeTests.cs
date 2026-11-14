using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Data.Migrations;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Tests.Integration.Fixtures;
using static Tubeshade.Server.Tests.Integration.AccessControl.AccessGrantsMigrationTests;

namespace Tubeshade.Server.Tests.Integration.AccessControl;

/// <summary>Upgrades the same data as <see cref="AccessGrantsMigrationTests"/> to the latest schema, and checks it through the repositories.</summary>
public sealed class AccessGrantsUpgradeTests(ServerFixture fixture) : ServerTests(fixture)
{
    private IServiceProvider _services = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var connectionString = await MigrationDatabase.CreateAsync(Fixture);
        MigrationDatabase.Migrate(connectionString, 60);

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.ExecuteAsync(OldSchemaData, OldSchemaParameters);
        }

        _services = ServerFixture.CreateServices(connectionString);
        await using var scope = _services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>().Migrate();
    }

    [Test]
    public async Task UsersKeepTheirIds()
    {
        await using var scope = _services.CreateAsyncScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();

        var alice = await userRepository.FindByIdAsync(Alice);
        alice.Should().NotBeNull();
        alice!.PublicId.Should().Be(Alice);
        alice.Name.Should().Be("alice");
    }

    [Test]
    public async Task AccessFollowsGrants()
    {
        await using var scope = _services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();
        var videoRepository = scope.ServiceProvider.GetRequiredService<VideoRepository>();
        var libraryRepository = scope.ServiceProvider.GetRequiredService<LibraryRepository>();

        var alice = await UserId(scope, Alice);
        var bob = await UserId(scope, Bob);
        var carol = await UserId(scope, Carol);
        var system = (await scope.ServiceProvider.GetRequiredService<UserRepository>().FindAsync("SYSTEM"))!.Id;

        using (new AssertionScope())
        {
            (await videoRepository.FindAsync(VideoId, alice, Permissions.Read, transaction)).Should().NotBeNull();
            (await videoRepository.FindAsync(VideoId, bob, Permissions.Read, transaction)).Should().NotBeNull();
            (await videoRepository.FindAsync(VideoId, carol, Permissions.Read, transaction)).Should().BeNull("modify never included read");
            (await videoRepository.FindAsync(VideoId, system, Permissions.Read, transaction)).Should().BeNull();
        }

        // Update without Read still allows a blind update
        var library = (await libraryRepository.FindAsync(LibraryId, alice, Permissions.Read, transaction))!;
        library.PublicId.Should().Be(LibraryId);
        (await libraryRepository.UpdateAsync(library with { ModifiedByUserId = carol, Name = "renamed" }, transaction)).Should().Be(1);
        (await libraryRepository.DeleteAsync(LibraryId, carol, transaction)).Should().Be(0);

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task TasksKeepTheirReferences()
    {
        await using var scope = _services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();
        var taskRepository = scope.ServiceProvider.GetRequiredService<TaskRepository>();
        var alice = await UserId(scope, Alice);

        var feedUpdate = await taskRepository.FindAsync(FeedUpdateTaskId, alice, Permissions.Read, transaction);
        using (new AssertionScope())
        {
            feedUpdate.Should().NotBeNull();
            feedUpdate!.PublicId.Should().Be(FeedUpdateTaskId);
            feedUpdate.LibraryId.Should().Be(LibraryId);
            feedUpdate.ChannelId.Should().Be(ChannelId);
            feedUpdate.UserId.Should().Be(alice);
        }

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task NewUsersGetPersonalOwner()
    {
        var user = await ServerFixture.CreateUserAsync(_services, $"new-{Guid.NewGuid():N}");

        await using var scope = _services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();
        var libraryRepository = scope.ServiceProvider.GetRequiredService<LibraryRepository>();
        var videoRepository = scope.ServiceProvider.GetRequiredService<VideoRepository>();

        var libraryId = await CreateLibrary(user.Id, scope.ServiceProvider, transaction);
        var alice = await UserId(scope, Alice);

        using (new AssertionScope())
        {
            (await libraryRepository.FindAsync(libraryId, user.Id, Permissions.All, transaction)).Should().NotBeNull();
            (await libraryRepository.FindAsync(libraryId, alice, Permissions.Read, transaction)).Should().BeNull();
            (await videoRepository.FindAsync(VideoId, user.Id, Permissions.Read, transaction)).Should().BeNull();
        }

        await transaction.RollbackAsync();
    }

    private static async Task<long> UserId(AsyncServiceScope scope, Guid publicId)
    {
        var user = await scope.ServiceProvider.GetRequiredService<UserRepository>().FindByIdAsync(publicId);
        return user!.Id;
    }
}
