using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Preferences;

public sealed class PreferencesRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<PreferencesEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.preferences";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO media.preferences (created_by_user_id, modified_by_user_id, playback_speed, videos_count, live_streams_count, shorts_count, player_client, download_automatically)
         VALUES (@CreatedByUserId, @ModifiedByUserId, @PlaybackSpeed, @VideosCount, @LiveStreamsCount, @ShortsCount, @PlayerClient, @DownloadAutomatically)
         RETURNING id;
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        $"""
         SELECT preferences.id AS {nameof(PreferencesEntity.Id)},
                preferences.created_at AS {nameof(PreferencesEntity.CreatedAt)},
                preferences.created_by_user_id AS {nameof(PreferencesEntity.CreatedByUserId)},
                preferences.modified_at AS {nameof(PreferencesEntity.ModifiedAt)},
                preferences.modified_by_user_id AS {nameof(PreferencesEntity.ModifiedByUserId)},
                preferences.playback_speed AS {nameof(PreferencesEntity.PlaybackSpeed)},
                preferences.videos_count AS {nameof(PreferencesEntity.VideosCount)},
                preferences.live_streams_count AS {nameof(PreferencesEntity.LiveStreamsCount)},
                preferences.shorts_count AS {nameof(PreferencesEntity.ShortsCount)},
                preferences.player_client AS {nameof(PreferencesEntity.PlayerClient)},
                preferences.download_automatically AS {nameof(PreferencesEntity.DownloadAutomatically)}
         FROM media.preferences
         """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        $"""
         playback_speed = @{nameof(PreferencesEntity.PlaybackSpeed)},
         videos_count = @{nameof(PreferencesEntity.VideosCount)},
         live_streams_count = @{nameof(PreferencesEntity.LiveStreamsCount)},
         shorts_count = @{nameof(PreferencesEntity.ShortsCount)},
         player_client = @{nameof(PreferencesEntity.PlayerClient)},
         download_automatically = @{nameof(PreferencesEntity.DownloadAutomatically)}
         """;

    /// <inheritdoc />
    protected override string AccessCte => string.Empty;

    /// <inheritdoc />
    protected override string UpdateAccessCte => string.Empty;

    /// <inheritdoc />
    protected override string AccessFilter => "(1 = 1)";

    public async ValueTask<PreferencesEntity?> FindForLibrary(
        Guid libraryId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            // lang=sql
            $"""
             SELECT preferences.id AS {nameof(PreferencesEntity.Id)},
                    preferences.created_at AS {nameof(PreferencesEntity.CreatedAt)},
                    preferences.created_by_user_id AS {nameof(PreferencesEntity.CreatedByUserId)},
                    preferences.modified_at AS {nameof(PreferencesEntity.ModifiedAt)},
                    preferences.modified_by_user_id AS {nameof(PreferencesEntity.ModifiedByUserId)},
                    preferences.playback_speed AS {nameof(PreferencesEntity.PlaybackSpeed)},
                    preferences.videos_count AS {nameof(PreferencesEntity.VideosCount)},
                    preferences.live_streams_count AS {nameof(PreferencesEntity.LiveStreamsCount)},
                    preferences.shorts_count AS {nameof(PreferencesEntity.ShortsCount)},
                    preferences.player_client AS {nameof(PreferencesEntity.PlayerClient)},
                    preferences.download_automatically AS {nameof(PreferencesEntity.DownloadAutomatically)}
             FROM media.libraries
                INNER JOIN media.library_preferences ON libraries.id = library_preferences.library_id
                INNER JOIN media.preferences ON library_preferences.preference_id = preferences.id
             WHERE libraries.id = @{nameof(libraryId)};
             """,
            new { libraryId, userId },
            transaction));
    }

    public async ValueTask<PreferencesEntity?> FindForLibrary(
        Guid libraryId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            // lang=sql
            $"""
             SELECT preferences.id AS {nameof(PreferencesEntity.Id)},
                    preferences.created_at AS {nameof(PreferencesEntity.CreatedAt)},
                    preferences.created_by_user_id AS {nameof(PreferencesEntity.CreatedByUserId)},
                    preferences.modified_at AS {nameof(PreferencesEntity.ModifiedAt)},
                    preferences.modified_by_user_id AS {nameof(PreferencesEntity.ModifiedByUserId)},
                    preferences.playback_speed AS {nameof(PreferencesEntity.PlaybackSpeed)},
                    preferences.videos_count AS {nameof(PreferencesEntity.VideosCount)},
                    preferences.live_streams_count AS {nameof(PreferencesEntity.LiveStreamsCount)},
                    preferences.shorts_count AS {nameof(PreferencesEntity.ShortsCount)},
                    preferences.player_client AS {nameof(PreferencesEntity.PlayerClient)},
                    preferences.download_automatically AS {nameof(PreferencesEntity.DownloadAutomatically)}
             FROM media.libraries
                INNER JOIN media.library_preferences ON libraries.id = library_preferences.library_id
                INNER JOIN media.preferences ON library_preferences.preference_id = preferences.id
             WHERE libraries.id = @{nameof(libraryId)};
             """,
            new { libraryId, userId },
            cancellationToken: cancellationToken));
    }


    public async ValueTask<int> LinkToLibrary(
        Guid preferenceId,
        Guid libraryId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            new CommandDefinition(
                $"""
                 INSERT INTO media.library_preferences (library_id, preference_id)
                 VALUES (@{nameof(libraryId)}, @{nameof(preferenceId)});
                 """,
                new { preferenceId, libraryId, userId },
                transaction));
    }

    public async ValueTask<PreferencesEntity?> GetEffectiveForLibrary(
        Guid libraryId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            $"""
             SELECT library_p.id AS Id,
                    library_p.created_at AS CreatedAt,
                    library_p.created_by_user_id AS CreatedByUserId,
                    library_p.modified_at AS ModifiedAt,
                    library_p.modified_by_user_id AS ModifiedByUserId,
                    library_p.playback_speed AS PlaybackSpeed,
                    library_p.videos_count AS VideosCount,
                    library_p.live_streams_count AS LiveStreamsCount,
                    library_p.shorts_count AS ShortsCount,
                    library_p.player_client AS PlayerClient,
                    library_p.download_automatically AS {nameof(PreferencesEntity.DownloadAutomatically)}
             FROM media.libraries
                      LEFT OUTER JOIN media.library_preferences ON libraries.id = library_preferences.library_id
                      LEFT OUTER JOIN media.preferences library_p ON library_preferences.preference_id = library_p.id
             WHERE libraries.id = @{nameof(libraryId)};
             """,
            new { libraryId, userId },
            cancellationToken: cancellationToken));
    }

    public async ValueTask<PreferencesEntity?> FindForChannel(
        Guid channelId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            // lang=sql
            $"""
             SELECT preferences.id AS {nameof(PreferencesEntity.Id)},
                    preferences.created_at AS {nameof(PreferencesEntity.CreatedAt)},
                    preferences.created_by_user_id AS {nameof(PreferencesEntity.CreatedByUserId)},
                    preferences.modified_at AS {nameof(PreferencesEntity.ModifiedAt)},
                    preferences.modified_by_user_id AS {nameof(PreferencesEntity.ModifiedByUserId)},
                    preferences.playback_speed AS {nameof(PreferencesEntity.PlaybackSpeed)},
                    preferences.videos_count AS {nameof(PreferencesEntity.VideosCount)},
                    preferences.live_streams_count AS {nameof(PreferencesEntity.LiveStreamsCount)},
                    preferences.shorts_count AS {nameof(PreferencesEntity.ShortsCount)},
                    preferences.player_client AS {nameof(PreferencesEntity.PlayerClient)},
                    preferences.download_automatically AS {nameof(PreferencesEntity.DownloadAutomatically)}
             FROM media.channels
                INNER JOIN media.channel_preferences ON channels.id = channel_preferences.channel_id
                INNER JOIN media.preferences ON channel_preferences.preference_id = preferences.id
             WHERE channels.id = @{nameof(channelId)};
             """,
            new { channelId, userId },
            transaction));
    }

    public async ValueTask<PreferencesEntity?> FindForChannel(
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            // lang=sql
            $"""
             SELECT preferences.id AS {nameof(PreferencesEntity.Id)},
                    preferences.created_at AS {nameof(PreferencesEntity.CreatedAt)},
                    preferences.created_by_user_id AS {nameof(PreferencesEntity.CreatedByUserId)},
                    preferences.modified_at AS {nameof(PreferencesEntity.ModifiedAt)},
                    preferences.modified_by_user_id AS {nameof(PreferencesEntity.ModifiedByUserId)},
                    preferences.playback_speed AS {nameof(PreferencesEntity.PlaybackSpeed)},
                    preferences.videos_count AS {nameof(PreferencesEntity.VideosCount)},
                    preferences.live_streams_count AS {nameof(PreferencesEntity.LiveStreamsCount)},
                    preferences.shorts_count AS {nameof(PreferencesEntity.ShortsCount)},
                    preferences.player_client AS {nameof(PreferencesEntity.PlayerClient)},
                    preferences.download_automatically AS {nameof(PreferencesEntity.DownloadAutomatically)}
             FROM media.channels
                INNER JOIN media.channel_preferences ON channels.id = channel_preferences.channel_id
                INNER JOIN media.preferences ON channel_preferences.preference_id = preferences.id
             WHERE channels.id = @{nameof(channelId)};
             """,
            new { channelId, userId },
            cancellationToken: cancellationToken));
    }


    public async ValueTask<int> LinkToChannel(
        Guid preferenceId,
        Guid channelId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            new CommandDefinition(
                $"""
                 INSERT INTO media.channel_preferences (channel_id, preference_id)
                 VALUES (@{nameof(channelId)}, @{nameof(preferenceId)});
                 """,
                new { preferenceId, channelId, userId },
                transaction));
    }

    public async ValueTask<PreferencesEntity?> GetEffectiveForChannel(
        Guid libraryId,
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            $"""
             SELECT COALESCE(channel_p.id, library_p.id) AS Id,
                    COALESCE(channel_p.created_at, library_p.created_at) AS CreatedAt,
                    COALESCE(channel_p.created_by_user_id, library_p.created_by_user_id) AS CreatedByUserId,
                    COALESCE(channel_p.modified_at, library_p.modified_at) AS ModifiedAt,
                    COALESCE(channel_p.modified_by_user_id, library_p.modified_by_user_id) AS ModifiedByUserId,
                    COALESCE(channel_p.playback_speed, library_p.playback_speed) AS PlaybackSpeed,
                    COALESCE(channel_p.videos_count, library_p.videos_count) AS VideosCount,
                    COALESCE(channel_p.live_streams_count, library_p.live_streams_count) AS LiveStreamsCount,
                    COALESCE(channel_p.shorts_count, library_p.shorts_count) AS ShortsCount,
                    COALESCE(channel_p.player_client, library_p.player_client) AS PlayerClient,
                    COALESCE(channel_p.download_automatically, library_p.download_automatically) AS {nameof(PreferencesEntity.DownloadAutomatically)}
             FROM media.channels
                      LEFT OUTER JOIN media.channel_preferences ON channels.id = channel_preferences.channel_id
                      LEFT OUTER JOIN media.preferences channel_p ON channel_preferences.preference_id = channel_p.id

                      INNER JOIN media.libraries ON libraries.id = @{nameof(libraryId)}
                      LEFT OUTER JOIN media.library_preferences ON libraries.id = library_preferences.library_id
                      LEFT OUTER JOIN media.preferences library_p ON library_preferences.preference_id = library_p.id
             WHERE channels.id = @{nameof(channelId)} AND COALESCE(channel_p.id, library_p.id) IS NOT NULL;
             """,
            new { libraryId, channelId, userId },
            cancellationToken: cancellationToken));
    }

    public async ValueTask<PreferencesEntity?> GetEffectiveForVideo(
        Guid libraryId,
        Guid videoId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            $"""
             SELECT COALESCE(channel_p.id, library_p.id) AS Id,
                    COALESCE(channel_p.created_at, library_p.created_at) AS CreatedAt,
                    COALESCE(channel_p.created_by_user_id, library_p.created_by_user_id) AS CreatedByUserId,
                    COALESCE(channel_p.modified_at, library_p.modified_at) AS ModifiedAt,
                    COALESCE(channel_p.modified_by_user_id, library_p.modified_by_user_id) AS ModifiedByUserId,
                    COALESCE(channel_p.playback_speed, library_p.playback_speed) AS PlaybackSpeed,
                    COALESCE(channel_p.videos_count, library_p.videos_count) AS VideosCount,
                    COALESCE(channel_p.live_streams_count, library_p.live_streams_count) AS LiveStreamsCount,
                    COALESCE(channel_p.shorts_count, library_p.shorts_count) AS ShortsCount,
                    COALESCE(channel_p.player_client, library_p.player_client) AS PlayerClient,
                    COALESCE(channel_p.download_automatically, library_p.download_automatically) AS {nameof(PreferencesEntity.DownloadAutomatically)}
             FROM media.videos
                      INNER JOIN media.channels ON videos.channel_id = channels.id
                      LEFT OUTER JOIN media.channel_preferences ON channels.id = channel_preferences.channel_id
                      LEFT OUTER JOIN media.preferences channel_p ON channel_preferences.preference_id = channel_p.id

                      INNER JOIN media.libraries ON libraries.id = @{nameof(libraryId)}
                      LEFT OUTER JOIN media.library_preferences ON libraries.id = library_preferences.library_id
                      LEFT OUTER JOIN media.preferences library_p ON library_preferences.preference_id = library_p.id
             WHERE videos.id = @{nameof(videoId)} AND COALESCE(channel_p.id, library_p.id) IS NOT NULL;
             """,
            new { libraryId, videoId, userId },
            cancellationToken: cancellationToken));
    }
}
