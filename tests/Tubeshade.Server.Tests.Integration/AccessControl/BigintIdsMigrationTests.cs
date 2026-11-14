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

/// <summary>Upgrades a database with data in every table from uuid keys to bigint keys with uuid public ids.</summary>
/// <remarks>Stops at the bigint id migrations and checks the result with SQL, so that later migrations do not change what it tests.</remarks>
public sealed class BigintIdsMigrationTests(ServerFixture fixture) : ServerTests(fixture)
{
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();
    private static readonly Guid ClaimId = Guid.NewGuid();
    private static readonly Guid TaskId = Guid.NewGuid();
    private static readonly Guid ScheduleId = Guid.NewGuid();
    private static readonly Guid LibraryId = Guid.NewGuid();
    private static readonly Guid LibraryPreferencesId = Guid.NewGuid();
    private static readonly Guid ChannelPreferencesId = Guid.NewGuid();
    private static readonly Guid ChannelId = Guid.NewGuid();
    private static readonly Guid CreatorId = Guid.NewGuid();
    private static readonly Guid VideoId = Guid.NewGuid();
    private static readonly Guid PlaylistId = Guid.NewGuid();
    private static readonly Guid VideoImageId = Guid.NewGuid();
    private static readonly Guid PlaylistImageId = Guid.NewGuid();
    private static readonly Guid ChannelImageId = Guid.NewGuid();
    private static readonly Guid VideoFileId = Guid.NewGuid();
    private static readonly Guid TrackFileId = Guid.NewGuid();
    private static readonly Guid SegmentId = Guid.NewGuid();
    private static readonly Guid FinishedRunId = Guid.NewGuid();
    private static readonly Guid RunningRunId = Guid.NewGuid();

    private static readonly string Callback = $"https://tubeshade.example/api/v1.0/YouTube/Notifications/{ChannelId}";

    private string _connectionString = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _connectionString = await MigrationDatabase.CreateAsync(Fixture);
        MigrationDatabase.Migrate(_connectionString, 61);

        await using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.ExecuteAsync(
                OldSchemaData,
                new
                {
                    Alice, Bob, ClaimId, TaskId, ScheduleId, LibraryId, LibraryPreferencesId, ChannelPreferencesId, ChannelId,
                    CreatorId, VideoId, PlaylistId, VideoImageId, PlaylistImageId, ChannelImageId, VideoFileId, TrackFileId,
                    SegmentId, FinishedRunId, RunningRunId, Callback,
                });
        }

        MigrationDatabase.Migrate(_connectionString, 63);
    }

    [Test]
    public async Task UsersShareTheIdOfTheirPrincipal()
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        var principals = (await connection.QueryAsync<(Guid PublicId, string Kind, string Name)>(
                """
                SELECT principals.public_id, principals.kind::text, users.name
                FROM access.principals
                    INNER JOIN identity.users ON users.id = principals.id;
                """))
            .ToList();

        using (new AssertionScope())
        {
            principals.Should().Contain((Alice, "user", "alice"));
            principals.Should().Contain((Bob, "user", "bob"));
            principals.Should().ContainSingle(principal => principal.Kind == "system").Which.Name.Should().Be("system");
            (await connection.QuerySingleAsync<int>("SELECT count(*) FROM identity.users;")).Should().Be(principals.Count);
        }
    }

    [Test]
    public async Task PublicIdsKeepTheUuids()
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        // lang=sql
        const string sql =
            """
            SELECT ARRAY[(SELECT public_id FROM access.owners WHERE personal_for = (SELECT id FROM access.principals WHERE public_id = @Alice)) = @Alice,
                   EXISTS(SELECT 1 FROM identity.claims WHERE public_id = @ClaimId),
                   EXISTS(SELECT 1 FROM tasks.tasks WHERE public_id = @TaskId),
                   EXISTS(SELECT 1 FROM tasks.schedules WHERE public_id = @ScheduleId),
                   EXISTS(SELECT 1 FROM tasks.task_runs WHERE public_id = @FinishedRunId),
                   EXISTS(SELECT 1 FROM media.libraries WHERE public_id = @LibraryId AND storage_path = '/library'),
                   EXISTS(SELECT 1 FROM media.preferences WHERE public_id = @LibraryPreferencesId),
                   EXISTS(SELECT 1 FROM media.channels WHERE public_id = @ChannelId AND storage_path = '/library/channel'),
                   EXISTS(SELECT 1 FROM media.creators WHERE public_id = @CreatorId),
                   EXISTS(SELECT 1 FROM media.videos WHERE public_id = @VideoId AND storage_path = '/library/channel/video'),
                   EXISTS(SELECT 1 FROM media.playlists WHERE public_id = @PlaylistId),
                   EXISTS(SELECT 1 FROM media.image_files WHERE public_id = @VideoImageId),
                   EXISTS(SELECT 1 FROM media.video_files WHERE public_id = @VideoFileId),
                   EXISTS(SELECT 1 FROM media.track_files WHERE public_id = @TrackFileId),
                   EXISTS(SELECT 1 FROM media.sponsorblock_segments WHERE public_id = @SegmentId)];
            """;

        var found = await connection.QuerySingleAsync<bool[]>(
            sql,
            new
            {
                Alice, ClaimId, TaskId, ScheduleId, FinishedRunId, LibraryId, LibraryPreferencesId, ChannelId, CreatorId, VideoId,
                PlaylistId, VideoImageId, VideoFileId, TrackFileId, SegmentId,
            });

        found.Should().HaveCount(15).And.AllBeEquivalentTo(true);
    }

    [Test]
    public async Task ReferencesPointToTheSameRows()
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        // lang=sql
        const string sql =
            """
            SELECT ARRAY[(SELECT channels.public_id FROM media.videos INNER JOIN media.channels ON channels.id = videos.channel_id WHERE videos.public_id = @VideoId) = @ChannelId,
                   (SELECT owners.public_id FROM media.videos INNER JOIN access.owners ON owners.id = videos.owner_id WHERE videos.public_id = @VideoId) = @Alice,
                   (SELECT principals.public_id FROM media.videos INNER JOIN access.principals ON principals.id = videos.ignored_by_user_id WHERE videos.public_id = @VideoId) = @Bob,
                   (SELECT principals.public_id FROM media.channels INNER JOIN access.principals ON principals.id = channels.created_by_user_id WHERE channels.public_id = @ChannelId) = @Alice,
                   (SELECT schedules.public_id FROM media.libraries INNER JOIN tasks.schedules ON schedules.id = libraries.subscriptions_schedule_id WHERE libraries.public_id = @LibraryId) = @ScheduleId,
                   (SELECT tasks.public_id FROM tasks.schedules INNER JOIN tasks.tasks ON tasks.id = schedules.task_id WHERE schedules.public_id = @ScheduleId) = @TaskId,
                   (SELECT libraries.public_id FROM tasks.tasks INNER JOIN media.libraries ON libraries.id = tasks.library_id WHERE tasks.public_id = @TaskId) = @LibraryId,
                   (SELECT principals.public_id FROM tasks.tasks INNER JOIN access.principals ON principals.id = tasks.user_id WHERE tasks.public_id = @TaskId) = @Alice,
                   (SELECT libraries.public_id FROM media.library_channels INNER JOIN media.libraries ON libraries.id = library_channels.library_id INNER JOIN media.channels ON channels.id = library_channels.channel_id WHERE channels.public_id = @ChannelId) = @LibraryId,
                   (SELECT creators.public_id FROM media.creator_channels INNER JOIN media.creators ON creators.id = creator_channels.creator_id INNER JOIN media.channels ON channels.id = creator_channels.channel_id WHERE channels.public_id = @ChannelId) = @CreatorId,
                   (SELECT preferences.public_id FROM media.library_preferences INNER JOIN media.preferences ON preferences.id = library_preferences.preference_id INNER JOIN media.libraries ON libraries.id = library_preferences.library_id WHERE libraries.public_id = @LibraryId) = @LibraryPreferencesId,
                   (SELECT preferences.public_id FROM media.channel_preferences INNER JOIN media.preferences ON preferences.id = channel_preferences.preference_id INNER JOIN media.channels ON channels.id = channel_preferences.channel_id WHERE channels.public_id = @ChannelId) = @ChannelPreferencesId,
                   (SELECT image_files.public_id FROM media.video_images INNER JOIN media.image_files ON image_files.id = video_images.image_id INNER JOIN media.videos ON videos.id = video_images.video_id WHERE videos.public_id = @VideoId) = @VideoImageId,
                   (SELECT image_files.public_id FROM media.channel_images INNER JOIN media.image_files ON image_files.id = channel_images.image_id INNER JOIN media.channels ON channels.id = channel_images.channel_id WHERE channels.public_id = @ChannelId) = @ChannelImageId,
                   (SELECT image_files.public_id FROM media.playlist_images INNER JOIN media.image_files ON image_files.id = playlist_images.image_id INNER JOIN media.playlists ON playlists.id = playlist_images.playlist_id WHERE playlists.public_id = @PlaylistId) = @PlaylistImageId,
                   (SELECT videos.public_id FROM media.playlist_videos INNER JOIN media.videos ON videos.id = playlist_videos.video_id INNER JOIN media.playlists ON playlists.id = playlist_videos.playlist_id WHERE playlists.public_id = @PlaylistId) = @VideoId,
                   (SELECT videos.public_id FROM media.video_files INNER JOIN media.videos ON videos.id = video_files.video_id WHERE video_files.public_id = @VideoFileId) = @VideoId,
                   (SELECT task_runs.public_id FROM media.video_files_downloading INNER JOIN tasks.task_runs ON task_runs.id = video_files_downloading.task_run_id) = @RunningRunId,
                   (SELECT videos.public_id FROM media.track_files INNER JOIN media.videos ON videos.id = track_files.video_id WHERE track_files.public_id = @TrackFileId) = @VideoId,
                   (SELECT videos.public_id FROM media.sponsorblock_segments INNER JOIN media.videos ON videos.id = sponsorblock_segments.video_id WHERE sponsorblock_segments.public_id = @SegmentId) = @VideoId,
                   (SELECT principals.public_id FROM media.video_plays INNER JOIN access.principals ON principals.id = video_plays.user_id) = @Alice,
                   (SELECT principals.public_id FROM media.video_viewed_by_users INNER JOIN access.principals ON principals.id = video_viewed_by_users.user_id) = @Alice,
                   (SELECT principals.public_id FROM identity.claims INNER JOIN access.principals ON principals.id = claims.user_id WHERE claims.public_id = @ClaimId) = @Alice,
                   (SELECT principals.public_id FROM identity.user_logins INNER JOIN access.principals ON principals.id = user_logins.user_id WHERE provider_key = 'alice') = @Alice,
                   (SELECT count(*) FROM tasks.task_run_results INNER JOIN tasks.task_runs ON task_runs.id = task_run_results.run_id WHERE task_runs.public_id = @FinishedRunId) = 1,
                   (SELECT count(*) FROM tasks.task_run_progress INNER JOIN tasks.task_runs ON task_runs.id = task_run_progress.run_id WHERE task_runs.public_id = @RunningRunId) = 1,
                   (SELECT count(*) FROM tasks.task_requests INNER JOIN tasks.tasks ON tasks.id = task_requests.task_id WHERE tasks.public_id = @TaskId) = 1];
            """;

        var found = await connection.QuerySingleAsync<bool[]>(
            sql,
            new
            {
                Alice, Bob, ClaimId, TaskId, ScheduleId, LibraryId, LibraryPreferencesId, ChannelPreferencesId, ChannelId,
                CreatorId, VideoId, PlaylistId, VideoImageId, PlaylistImageId, ChannelImageId, VideoFileId, TrackFileId,
                SegmentId, FinishedRunId, RunningRunId,
            });

        found.Should().HaveCount(27).And.AllBeEquivalentTo(true);
    }

    [Test]
    public async Task KeyedByAnotherTable()
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        var subscription = await connection.QuerySingleAsync<(Guid ChannelId, string Callback)>(
            """
            SELECT channels.public_id, channel_subscriptions.callback
            FROM media.channel_subscriptions
                INNER JOIN media.channels ON channels.id = channel_subscriptions.channel_id;
            """);

        var cookieLibrary = await connection.QuerySingleAsync<Guid>(
            """
            SELECT libraries.public_id
            FROM media.library_external_cookies
                INNER JOIN media.libraries ON libraries.id = library_external_cookies.library_id;
            """);

        using (new AssertionScope())
        {
            subscription.Should().Be((ChannelId, Callback), "webhook callbacks keep the channel's public id");
            cookieLibrary.Should().Be(LibraryId);
        }
    }

    [Test]
    public async Task GrantsReferencePrincipals()
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        var bobOnAlice = await connection.QuerySingleAsync<int>(
            """
            SELECT grants.permissions
            FROM access.grants
                INNER JOIN access.principals ON principals.id = grants.principal_id
                INNER JOIN access.owners ON owners.id = grants.owner_id
            WHERE principals.public_id = @Bob
              AND owners.public_id = @Alice;
            """,
            new { Alice, Bob });

        var readableByBob = await connection.QueryAsync<Guid>(
            """
            SELECT owners.public_id
            FROM access.accessible_owners((SELECT id FROM access.principals WHERE public_id = @Bob), @Permissions) accessible
                INNER JOIN access.owners ON owners.id = accessible.owner_id;
            """,
            new { Bob, Permissions = (int)Permissions.Read });

        using (new AssertionScope())
        {
            ((Permissions)bobOnAlice).Should().Be(Permissions.Read);
            readableByBob.Should().BeEquivalentTo([Alice, Bob]);
        }
    }

    [Test]
    public async Task OnlyPublicIdsAreUuids()
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        var uuidColumns = await connection.QueryAsync<string>(
            """
            SELECT table_schema || '.' || table_name || '.' || column_name
            FROM information_schema.columns
            WHERE table_schema IN ('access', 'identity', 'media', 'tasks')
              AND data_type = 'uuid'
              AND column_name NOT IN ('public_id', 'security_stamp', 'concurrency_stamp');
            """);

        var foreignKeys = await connection.QueryAsync<(string Constraint, string Type, string Referenced)>(
            """
            SELECT constraints.conname,
                   format_type(source.atttypid, source.atttypmod),
                   target.attname
            FROM pg_constraint constraints
                INNER JOIN pg_attribute source ON source.attrelid = constraints.conrelid AND source.attnum = constraints.conkey[1]
                INNER JOIN pg_attribute target ON target.attrelid = constraints.confrelid AND target.attnum = constraints.confkey[1]
            WHERE constraints.contype = 'f'
              AND constraints.connamespace::regnamespace::text IN ('access', 'identity', 'media', 'tasks');
            """);

        using (new AssertionScope())
        {
            uuidColumns.Should().BeEmpty();
            foreignKeys.Should().NotBeEmpty().And.OnlyContain(key => key.Type == "bigint" && key.Referenced == "id");
        }
    }

    /// <summary>A row in every table, in the schema after migration 61.</summary>
    // lang=sql
    private const string OldSchemaData =
        """
        WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
        INSERT INTO identity.users (id, created_by_user_id, modified_by_user_id, name, normalized_name)
        SELECT new_users.id, system.id, system.id, new_users.name, upper(new_users.name)
        FROM system,
             (VALUES (@Alice, 'alice'), (@Bob, 'bob')) AS new_users(id, name);

        INSERT INTO identity.user_logins (provider_key, user_id, login_provider) VALUES ('alice', @Alice, 'oidc');
        INSERT INTO identity.claims (id, created_by_user_id, user_id, claim_type, claim_value) VALUES (@ClaimId, @Alice, @Alice, 'role', 'admin');

        INSERT INTO access.owners (id, created_by_user_id, modified_by_user_id, name, personal_for)
        VALUES (@Alice, @Alice, @Alice, 'alice', @Alice),
               (@Bob, @Bob, @Bob, 'bob', @Bob);

        INSERT INTO access.grants (principal_id, owner_id, permissions, granted_by)
        VALUES (@Alice, @Alice, 31, @Alice),
               (@Bob, @Bob, 31, @Bob),
               (@Bob, @Alice, 1, @Alice);

        INSERT INTO tasks.tasks (id, created_by_user_id, modified_by_user_id, owner_id, type, user_id)
        VALUES (@TaskId, @Alice, @Alice, @Alice, 'scan_subscriptions', @Alice);

        INSERT INTO tasks.schedules (id, created_by_user_id, modified_by_user_id, owner_id, task_id, cron_expression, time_zone_id)
        VALUES (@ScheduleId, @Alice, @Alice, @Alice, @TaskId, '0 * * * *', 'Etc/UTC');

        INSERT INTO media.libraries (id, created_by_user_id, modified_by_user_id, owner_id, name, storage_path, subscriptions_schedule_id)
        VALUES (@LibraryId, @Alice, @Alice, @Alice, 'library', '/library', @ScheduleId);

        UPDATE tasks.tasks SET library_id = @LibraryId WHERE id = @TaskId;

        INSERT INTO media.library_external_cookies (id, created_by_user_id, modified_by_user_id, domain, cookie)
        VALUES (@LibraryId, @Alice, @Alice, 'youtube.com', 'cookie');

        INSERT INTO media.preferences (id, created_by_user_id, modified_by_user_id, videos_count)
        VALUES (@LibraryPreferencesId, @Alice, @Alice, 5),
               (@ChannelPreferencesId, @Alice, @Alice, 7);

        INSERT INTO media.library_preferences (library_id, preference_id) VALUES (@LibraryId, @LibraryPreferencesId);

        INSERT INTO media.channels (id, created_by_user_id, modified_by_user_id, owner_id, name, storage_path, external_id, external_url, availability)
        VALUES (@ChannelId, @Alice, @Alice, @Alice, 'channel', '/library/channel', 'channel', 'https://example.org/channel', 'public');

        INSERT INTO media.library_channels (library_id, channel_id, "primary") VALUES (@LibraryId, @ChannelId, true);
        INSERT INTO media.channel_preferences (channel_id, preference_id) VALUES (@ChannelId, @ChannelPreferencesId);

        INSERT INTO media.channel_subscriptions (id, created_by_user_id, modified_by_user_id, status, callback, topic)
        VALUES (@ChannelId, @Alice, @Alice, 'subscribed', @Callback, 'https://www.youtube.com/xml/feeds/videos.xml?channel_id=channel');

        INSERT INTO media.creators (id, created_by_user_id, modified_by_user_id, owner_id, name)
        VALUES (@CreatorId, @Alice, @Alice, @Alice, 'creator');

        INSERT INTO media.creator_channels (creator_id, channel_id, "primary") VALUES (@CreatorId, @ChannelId, true);

        INSERT INTO media.videos (id, created_by_user_id, modified_by_user_id, owner_id, name, description, categories, tags, type, view_count, like_count, channel_id, storage_path, external_id, external_url, published_at, refreshed_at, availability, duration, ignored_at, ignored_by_user_id)
        VALUES (@VideoId, @Alice, @Alice, @Alice, 'video', '', '{}', '{}', 'video', 0, 0, @ChannelId, '/library/channel/video', 'video', 'https://example.org/video', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'public', 'PT10M', CURRENT_TIMESTAMP, @Bob);

        INSERT INTO media.playlists (id, created_by_user_id, modified_by_user_id, library_id, storage_path, external_id, external_url, name, refreshed_at)
        VALUES (@PlaylistId, @Alice, @Alice, @LibraryId, '/library/playlist', 'playlist', 'https://example.org/playlist', 'playlist', CURRENT_TIMESTAMP);

        INSERT INTO media.playlist_videos (playlist_id, video_id, "order") VALUES (@PlaylistId, @VideoId, 0);

        INSERT INTO media.image_files (id, created_by_user_id, modified_by_user_id, storage_path, type, width, height, hash, hash_algorithm, storage_size)
        VALUES (@VideoImageId, @Alice, @Alice, '/library/channel/video/thumbnail.jpg', 'thumbnail', 1, 1, '\x00', 'SHA256', 1),
               (@PlaylistImageId, @Alice, @Alice, '/library/playlist/thumbnail.jpg', 'thumbnail', 1, 1, '\x00', 'SHA256', 1),
               (@ChannelImageId, @Alice, @Alice, '/library/channel/banner.jpg', 'banner', 1, 1, '\x00', 'SHA256', 1);

        INSERT INTO media.video_images (video_id, image_id) VALUES (@VideoId, @VideoImageId);
        INSERT INTO media.playlist_images (playlist_id, image_id) VALUES (@PlaylistId, @PlaylistImageId);
        INSERT INTO media.channel_images (channel_id, image_id) VALUES (@ChannelId, @ChannelImageId);

        INSERT INTO media.video_files (id, created_by_user_id, modified_by_user_id, owner_id, video_id, storage_path, type, width, height, framerate, downloaded_by_user_id)
        VALUES (@VideoFileId, @Alice, @Alice, @Alice, @VideoId, '/library/channel/video/video.mp4', 'mp4', 1920, 1080, 30, @Alice);

        INSERT INTO media.track_files (id, created_by_user_id, modified_by_user_id, video_id, storage_path, type, language, hash, hash_algorithm, storage_size)
        VALUES (@TrackFileId, @Alice, @Alice, @VideoId, '/library/channel/video/video.en.vtt', 'subtitles', 'en', '\x00', 'SHA256', 1);

        INSERT INTO media.sponsorblock_segments (id, created_by_user_id, modified_by_user_id, video_id, external_id, start_time, end_time, category, action, locked)
        VALUES (@SegmentId, @Alice, @Alice, @VideoId, 'segment', 0, 1, 'sponsor', 'skip', false);

        INSERT INTO media.video_plays (video_id, user_id) VALUES (@VideoId, @Alice);
        INSERT INTO media.video_viewed_by_users (video_id, user_id, viewed) VALUES (@VideoId, @Alice, true);

        INSERT INTO tasks.task_runs (id, task_id, state, source)
        VALUES (@FinishedRunId, @TaskId, 'finished', 'user'),
               (@RunningRunId, @TaskId, 'running', 'schedule');

        INSERT INTO tasks.task_run_results (run_id, result) VALUES (@FinishedRunId, 'successful');
        INSERT INTO tasks.task_run_progress (run_id, value, target) VALUES (@RunningRunId, 1, 2);
        INSERT INTO tasks.task_requests (task_id, source) VALUES (@TaskId, 'user');

        INSERT INTO media.video_files_downloading (file_id, task_run_id, path) VALUES (@VideoFileId, @RunningRunId, '/tmp/video.mp4');
        """;
}
