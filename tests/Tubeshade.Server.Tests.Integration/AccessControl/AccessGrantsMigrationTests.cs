using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Dapper;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data.AccessControl;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.AccessControl;

/// <summary>Upgrades a database with data in the schema before access grants, as on a running instance.</summary>
/// <remarks>Stops at the access grants migration and checks the result with SQL, so that later migrations do not change what it tests.</remarks>
/// <seealso cref="AccessGrantsUpgradeTests"/>
public sealed class AccessGrantsMigrationTests(ServerFixture fixture) : ServerTests(fixture)
{
    internal static readonly Guid Alice = Guid.NewGuid();
    internal static readonly Guid Bob = Guid.NewGuid();
    internal static readonly Guid Carol = Guid.NewGuid();
    internal static readonly Guid LibraryId = Guid.NewGuid();
    internal static readonly Guid ChannelId = Guid.NewGuid();
    internal static readonly Guid VideoId = Guid.NewGuid();
    internal static readonly Guid ReindexTaskId = Guid.NewGuid();
    internal static readonly Guid FeedUpdateTaskId = Guid.NewGuid();
    internal static readonly Guid ReindexScheduleId = Guid.NewGuid();
    internal static readonly Guid SystemFileId = Guid.NewGuid();

    private string _connectionString = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _connectionString = await MigrationDatabase.CreateAsync(Fixture);
        MigrationDatabase.Migrate(_connectionString, 60);

        await using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.ExecuteAsync(OldSchemaData, OldSchemaParameters);
        }

        MigrationDatabase.Migrate(_connectionString, 61);
    }

    [Test]
    public async Task OwnershipsBecomeGrants()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var systemId = await SystemUserId(connection);

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
        await using var connection = new NpgsqlConnection(_connectionString);
        var systemId = await SystemUserId(connection);

        using (new AssertionScope())
        {
            (await CanAccessVideo(connection, Alice, Permissions.Read)).Should().BeTrue();
            (await CanAccessVideo(connection, Bob, Permissions.Read)).Should().BeTrue();
            (await CanAccessVideo(connection, Carol, Permissions.Read)).Should().BeFalse("modify never included read");
            (await CanAccessVideo(connection, systemId, Permissions.Read)).Should().BeFalse();

            // Update without Read still allows a blind update
            (await CanAccessVideo(connection, Carol, Permissions.Update)).Should().BeTrue();
            (await CanAccessVideo(connection, Carol, Permissions.Delete)).Should().BeFalse();
        }
    }

    [Test]
    public async Task SystemTasksRunAsLibraryOwner()
    {
        await using var connection = new NpgsqlConnection(_connectionString);

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
        var systemId = await SystemUserId(connection);
        var maintenanceOwners = await connection.QueryAsync<Guid>("SELECT owner_id FROM tasks.tasks WHERE library_id IS NULL;");
        maintenanceOwners.Should().NotBeEmpty().And.AllBeEquivalentTo(systemId);
    }

    [Test]
    public async Task FilesBelongToTheirVideosOwner()
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        var fileOwner = await connection.QuerySingleAsync<Guid>(
            "SELECT owner_id FROM media.video_files WHERE id = @SystemFileId;",
            new { SystemFileId });

        fileOwner.Should().Be(Alice, "the file was owned by the system user, not by the owner of its video");
    }

    internal static object OldSchemaParameters => new { Alice, Bob, Carol, LibraryId, ChannelId, VideoId, ReindexTaskId, FeedUpdateTaskId, ReindexScheduleId, SystemFileId };

    private static async Task<Permissions> Grant(NpgsqlConnection connection, Guid principalId, Guid ownerId)
    {
        var permissions = await connection.QuerySingleOrDefaultAsync<int?>(
            "SELECT permissions FROM access.grants WHERE principal_id = @principalId AND owner_id = @ownerId;",
            new { principalId, ownerId });

        return (Permissions)(permissions ?? 0);
    }

    private static async Task<bool> CanAccessVideo(NpgsqlConnection connection, Guid principalId, Permissions permissions)
    {
        return await connection.QuerySingleAsync<bool>(
            """
            SELECT EXISTS(SELECT 1
                          FROM media.videos
                          WHERE videos.id = @VideoId
                            AND videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@principalId, @permissions)));
            """,
            new { VideoId, principalId, permissions = (int)permissions });
    }

    private static async Task<Guid> SystemUserId(NpgsqlConnection connection)
    {
        return await connection.QuerySingleAsync<Guid>("SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM';");
    }

    /// <summary>Users, owners and ownerships as the application created them before migration 61, plus owned data.</summary>
    // lang=sql
    internal const string OldSchemaData =
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

        -- A file added while the system user owned it, on a video of alice
        WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
        INSERT INTO media.video_files (id, created_by_user_id, modified_by_user_id, owner_id, video_id, storage_path, type, width, height, framerate)
        SELECT @SystemFileId, system.id, system.id, system.id, @VideoId, '/library/channel/video/video.mp4', 'mp4', 1920, 1080, 30
        FROM system;
        """;
}
