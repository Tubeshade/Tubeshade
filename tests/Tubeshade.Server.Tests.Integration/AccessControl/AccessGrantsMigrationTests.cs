using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Dapper;
using DbUp;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Data.Migrations;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.AccessControl;

/// <summary>Upgrades a database with data in the schema before access grants, as on a running instance.</summary>
public sealed partial class AccessGrantsMigrationTests(ServerFixture fixture) : ServerTests(fixture)
{
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();
    private static readonly Guid Carol = Guid.NewGuid();
    private static readonly Guid LibraryId = Guid.NewGuid();
    private static readonly Guid ChannelId = Guid.NewGuid();
    private static readonly Guid VideoId = Guid.NewGuid();
    private static readonly Guid ReindexTaskId = Guid.NewGuid();
    private static readonly Guid FeedUpdateTaskId = Guid.NewGuid();
    private static readonly Guid ReindexScheduleId = Guid.NewGuid();

    private IServiceProvider _services = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var databaseName = $"upgrade_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(Fixture.ConnectionString))
        {
            await connection.ExecuteAsync($"CREATE DATABASE {databaseName};");
        }

        var connectionString = new NpgsqlConnectionStringBuilder(Fixture.ConnectionString) { Database = databaseName }.ConnectionString;

        var result = DeployChanges
            .To.PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrationService).Assembly, name => MigrationNumber(name) <= 60)
            .WithTransactionPerScript()
            .LogToNowhere()
            .Build()
            .PerformUpgrade();

        result.Successful.Should().BeTrue(result.Error?.ToString());
        result.Scripts.Should().Contain(script => script.Name.Contains("000060_"));

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.ExecuteAsync(OldSchemaData, new { Alice, Bob, Carol, LibraryId, ChannelId, VideoId, ReindexTaskId, FeedUpdateTaskId, ReindexScheduleId });
        }

        _services = ServerFixture.CreateServices(connectionString);
        await using var scope = _services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>().Migrate();
    }

    [Test]
    public async Task OwnershipsBecomeGrants()
    {
        await using var scope = _services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var systemId = await SystemUserId(scope);

        using (new AssertionScope())
        {
            (await Grant(connection, Alice, Alice)).Should().Be(Permissions.All);
            (await Grant(connection, Alice, systemId)).Should().Be(Permissions.Read);
            (await Grant(connection, Bob, Alice)).Should().Be(Permissions.Read | Permissions.Create | Permissions.Update | Permissions.Delete);
            (await Grant(connection, Bob, Bob)).Should().Be(Permissions.All);
            (await Grant(connection, Carol, Alice)).Should().Be(Permissions.Update);
            (await Grant(connection, systemId, Alice)).Should().Be(Permissions.None, "the system user no longer holds grants on users' owners");
            (await Grant(connection, systemId, Bob)).Should().Be(Permissions.None);
        }

        const string personalOwnerSql = "SELECT id FROM access.owners WHERE personal_for = @userId;";
        (await connection.QuerySingleAsync<Guid>(personalOwnerSql, new { userId = Alice })).Should().Be(Alice);
        (await connection.QuerySingleAsync<Guid>(personalOwnerSql, new { userId = systemId })).Should().Be(systemId);

        (await connection.QuerySingleAsync<bool>("SELECT to_regclass('identity.ownerships') IS NULL;")).Should().BeTrue();
        (await connection.QuerySingleAsync<bool>("SELECT to_regtype('identity.access') IS NULL;")).Should().BeTrue();
    }

    [Test]
    public async Task AccessFollowsGrants()
    {
        await using var scope = _services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();
        var videoRepository = scope.ServiceProvider.GetRequiredService<VideoRepository>();
        var libraryRepository = scope.ServiceProvider.GetRequiredService<LibraryRepository>();
        var systemId = await SystemUserId(scope);

        (await videoRepository.FindAsync(VideoId, Alice, Permissions.Read, transaction)).Should().NotBeNull();
        (await videoRepository.FindAsync(VideoId, Bob, Permissions.Read, transaction)).Should().NotBeNull();
        (await videoRepository.FindAsync(VideoId, Carol, Permissions.Read, transaction)).Should().BeNull("modify never included read");
        (await videoRepository.FindAsync(VideoId, systemId, Permissions.Read, transaction)).Should().BeNull();

        // Update without Read still allows a blind update
        var library = (await libraryRepository.FindAsync(LibraryId, Alice, Permissions.Read, transaction))!;
        (await libraryRepository.UpdateAsync(library with { ModifiedByUserId = Carol, Name = "renamed" }, transaction)).Should().Be(1);
        (await libraryRepository.DeleteAsync(LibraryId, Carol, transaction)).Should().Be(0);

        await transaction.RollbackAsync();
    }

    [Test]
    public async Task SystemTasksRunAsLibraryOwner()
    {
        await using var scope = _services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

        var tasks = (await connection.QueryAsync<(Guid Id, Guid OwnerId, Guid? UserId)>(
                "SELECT id, owner_id, user_id FROM tasks.tasks WHERE id = ANY(@Ids);",
                new { Ids = new[] { ReindexTaskId, FeedUpdateTaskId } }))
            .ToDictionary(task => task.Id);

        tasks[ReindexTaskId].Should().Be((ReindexTaskId, Alice, Alice));
        tasks[FeedUpdateTaskId].Should().Be((FeedUpdateTaskId, Alice, Alice));

        var scheduleOwner = await connection.QuerySingleAsync<Guid>(
            "SELECT owner_id FROM tasks.schedules WHERE id = @ReindexScheduleId;",
            new { ReindexScheduleId });
        scheduleOwner.Should().Be(Alice);

        // Maintenance tasks without a library stay with the system owner, which every user can read
        var systemId = await SystemUserId(scope);
        var maintenanceOwners = await connection.QueryAsync<Guid>("SELECT owner_id FROM tasks.tasks WHERE library_id IS NULL;");
        maintenanceOwners.Should().NotBeEmpty().And.AllBeEquivalentTo(systemId);
    }

    [Test]
    public async Task NewUsersGetPersonalOwner()
    {
        var user = await ServerFixture.CreateUserAsync(_services, $"new-{Guid.NewGuid():N}");
        user.OwnerId.Should().NotBe(user.Id, "the personal owner no longer shares the user's id");

        await using var scope = _services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        (await Grant(connection, user.Id, user.OwnerId)).Should().Be(Permissions.All);
        (await Grant(connection, user.Id, await SystemUserId(scope))).Should().Be(Permissions.Read);
        (await Grant(connection, user.Id, Alice)).Should().Be(Permissions.None);
    }

    private static async Task<Permissions> Grant(NpgsqlConnection connection, Guid principalId, Guid ownerId)
    {
        var permissions = await connection.QuerySingleOrDefaultAsync<int?>(
            "SELECT permissions FROM access.grants WHERE principal_id = @principalId AND owner_id = @ownerId;",
            new { principalId, ownerId });

        return (Permissions)(permissions ?? 0);
    }

    private static async Task<Guid> SystemUserId(AsyncServiceScope scope)
    {
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        return await connection.QuerySingleAsync<Guid>("SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM';");
    }

    private static int MigrationNumber(string scriptName)
    {
        var match = MigrationNumberRegex().Match(scriptName);
        return match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
    }

    [GeneratedRegex(@"\.Migrations\.(\d{6})_")]
    private static partial Regex MigrationNumberRegex();

    /// <summary>Users, owners and ownerships as the application created them before migration 61, plus owned data.</summary>
    // lang=sql
    private const string OldSchemaData =
        """
        WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
        INSERT INTO identity.users (id, created_by_user_id, modified_by_user_id, name, normalized_name)
        SELECT new_users.id, system.id, system.id, new_users.name, upper(new_users.name)
        FROM system,
             (VALUES (@Alice, 'alice'), (@Bob, 'bob'), (@Carol, 'carol')) AS new_users(id, name);

        -- OwnerRepository.AddDefaultForUserAsync: the owner shares the user's id
        INSERT INTO identity.owners (id, created_by_user_id, modified_by_user_id, name)
        SELECT id, id, id, name
        FROM identity.users
        WHERE id IN (@Alice, @Bob, @Carol);

        -- OwnershipRepository.AddDefaultForUserAsync
        WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
        INSERT INTO identity.ownerships (created_by_user_id, modified_by_user_id, owner_id, user_id, access)
        SELECT users.id, users.id, users.id, system.id, 'owner'::identity.access FROM identity.users, system WHERE users.id IN (@Alice, @Bob, @Carol)
        UNION ALL
        SELECT users.id, users.id, system.id, users.id, 'read' FROM identity.users, system WHERE users.id IN (@Alice, @Bob, @Carol)
        UNION ALL
        SELECT users.id, users.id, users.id, users.id, 'owner' FROM identity.users WHERE users.id IN (@Alice, @Bob, @Carol);

        -- Shared access: every non-owner access value, combined for bob, and modify alone for carol
        INSERT INTO identity.ownerships (created_by_user_id, modified_by_user_id, owner_id, user_id, access)
        VALUES (@Alice, @Alice, @Alice, @Bob, 'read'),
               (@Alice, @Alice, @Alice, @Bob, 'append'),
               (@Alice, @Alice, @Alice, @Bob, 'modify'),
               (@Alice, @Alice, @Alice, @Bob, 'delete'),
               (@Alice, @Alice, @Alice, @Carol, 'modify');

        -- A library created through the UI, with its subscription task and schedule
        WITH subscription_task AS
                 (INSERT INTO tasks.tasks (created_by_user_id, modified_by_user_id, owner_id, type, user_id)
                  VALUES (@Alice, @Alice, @Alice, 'scan_subscriptions', @Alice)
                  RETURNING id),
             subscription_schedule AS
                 (INSERT INTO tasks.schedules (created_by_user_id, modified_by_user_id, owner_id, task_id, cron_expression, time_zone_id)
                  SELECT @Alice, @Alice, @Alice, id, '0 * * * *', 'Etc/UTC'
                  FROM subscription_task
                  RETURNING id)
        INSERT INTO media.libraries (id, created_by_user_id, modified_by_user_id, owner_id, name, storage_path, subscriptions_schedule_id)
        SELECT @LibraryId, @Alice, @Alice, @Alice, 'library', '/library', id
        FROM subscription_schedule;

        UPDATE tasks.tasks SET library_id = @LibraryId WHERE type = 'scan_subscriptions' AND owner_id = @Alice;

        INSERT INTO media.channels (id, created_by_user_id, modified_by_user_id, owner_id, name, storage_path, external_id, external_url, availability)
        VALUES (@ChannelId, @Alice, @Alice, @Alice, 'channel', '/library/channel', 'channel', 'https://example.org/channel', 'public');

        INSERT INTO media.library_channels (library_id, channel_id, "primary")
        VALUES (@LibraryId, @ChannelId, true);

        INSERT INTO media.videos (id, created_by_user_id, modified_by_user_id, owner_id, name, description, categories, tags, type, view_count, like_count, channel_id, storage_path, external_id, external_url, published_at, refreshed_at, availability, duration)
        VALUES (@VideoId, @Alice, @Alice, @Alice, 'video', '', '{}', '{}', 'video', 0, 0, @ChannelId, '/library/channel/video', 'video', 'https://example.org/video', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'public', 'PT10M');

        -- Reindexing as created by 000031_reindex-videos-task.sql: runs as the system user
        WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
        INSERT INTO tasks.tasks (id, created_by_user_id, modified_by_user_id, owner_id, type, user_id, library_id)
        SELECT @ReindexTaskId, system.id, system.id, @Alice, 'reindex_videos', system.id, @LibraryId
        FROM system;

        WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
        INSERT INTO tasks.schedules (id, created_by_user_id, modified_by_user_id, task_id, cron_expression, time_zone_id, owner_id)
        SELECT @ReindexScheduleId, system.id, system.id, @ReindexTaskId, '* * * * *', 'Etc/UTC', system.id
        FROM system;

        -- A feed update created by the webhook: owned by and running as the system user
        WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
        INSERT INTO tasks.tasks (id, created_by_user_id, modified_by_user_id, owner_id, type, user_id, library_id, channel_id, payload)
        SELECT @FeedUpdateTaskId, system.id, system.id, system.id, 'youtube_feed_update', system.id, @LibraryId, @ChannelId, ''
        FROM system;
        """;
}
