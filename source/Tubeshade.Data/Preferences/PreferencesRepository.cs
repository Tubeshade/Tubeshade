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
        """
        INSERT INTO media.preferences (created_by_user_id, modified_by_user_id, playback_speed, videos_count, live_streams_count, shorts_count, player_client, download_videos, download_method)
        VALUES (@CreatedByUserId, @ModifiedByUserId, @PlaybackSpeed, @VideosCount, @LiveStreamsCount, @ShortsCount, @PlayerClient, @DownloadVideos, @DownloadMethod)
        RETURNING id;
        """;

    /// <inheritdoc />
    protected override string SelectSql =>
        $"""
         SELECT preferences.id,
                preferences.created_at,
                preferences.created_by_user_id,
                preferences.modified_at,
                preferences.modified_by_user_id,
                preferences.playback_speed,
                preferences.videos_count,
                preferences.live_streams_count,
                preferences.shorts_count,
                preferences.player_client,
                preferences.download_videos,
                preferences.download_method,
                preferences.formats
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
         download_videos = @{nameof(PreferencesEntity.DownloadVideos)},
         download_method = @{nameof(PreferencesEntity.DownloadMethod)},
         formats = @{nameof(PreferencesEntity.Formats)}
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
             SELECT preferences.id,
                    preferences.created_at,
                    preferences.created_by_user_id,
                    preferences.modified_at,
                    preferences.modified_by_user_id,
                    preferences.playback_speed,
                    preferences.videos_count,
                    preferences.live_streams_count,
                    preferences.shorts_count,
                    preferences.player_client,
                    preferences.download_videos,
                    preferences.download_method,
                    preferences.formats
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
             SELECT preferences.id,
                    preferences.created_at,
                    preferences.created_by_user_id,
                    preferences.modified_at,
                    preferences.modified_by_user_id,
                    preferences.playback_speed,
                    preferences.videos_count,
                    preferences.live_streams_count,
                    preferences.shorts_count,
                    preferences.player_client,
                    preferences.download_videos,
                    preferences.download_method,
                    preferences.formats
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
             SELECT library_p.id,
                    library_p.created_at,
                    library_p.created_by_user_id,
                    library_p.modified_at,
                    library_p.modified_by_user_id,
                    library_p.playback_speed,
                    library_p.videos_count,
                    library_p.live_streams_count,
                    library_p.shorts_count,
                    library_p.player_client,
                    library_p.download_videos,
                    library_p.download_method,
                    library_p.formats
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
             SELECT preferences.id,
                    preferences.created_at,
                    preferences.created_by_user_id,
                    preferences.modified_at,
                    preferences.modified_by_user_id,
                    preferences.playback_speed,
                    preferences.videos_count,
                    preferences.live_streams_count,
                    preferences.shorts_count,
                    preferences.player_client,
                    preferences.download_videos,
                    preferences.download_method,
                    preferences.formats
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
             SELECT preferences.id,
                    preferences.created_at,
                    preferences.created_by_user_id,
                    preferences.modified_at,
                    preferences.modified_by_user_id,
                    preferences.playback_speed,
                    preferences.videos_count,
                    preferences.live_streams_count,
                    preferences.shorts_count,
                    preferences.player_client,
                    preferences.download_videos,
                    preferences.download_method,
                    preferences.formats
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
                    COALESCE(channel_p.download_videos, library_p.download_videos) AS {nameof(PreferencesEntity.DownloadVideos)},
                    COALESCE(channel_p.download_method, library_p.download_method) AS {nameof(PreferencesEntity.DownloadMethod)},
                    COALESCE(channel_p.formats, library_p.formats) AS {nameof(PreferencesEntity.Formats)}
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
        NpgsqlTransaction transaction,
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
                    COALESCE(channel_p.download_videos, library_p.download_videos) AS {nameof(PreferencesEntity.DownloadVideos)},
                    COALESCE(channel_p.download_method, library_p.download_method) AS {nameof(PreferencesEntity.DownloadMethod)},
                    COALESCE(channel_p.formats, library_p.formats) AS {nameof(PreferencesEntity.Formats)}
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
            transaction,
            cancellationToken: cancellationToken));
    }
}
