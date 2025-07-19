using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Preferences;

public sealed class PreferencesRepository(NpgsqlConnection connection) : RepositoryBase<PreferencesEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.preferences";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO media.preferences (id, created_at, created_by_user_id, modified_at, modified_by_user_id, playback_speed) 
         VALUES (@Id, @CreatedAt, @CreatedByUserId, @ModifiedAt, @ModifiedByUserId, @PlaybackSpeed)
         RETURNING id;
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        $"""
         SELECT id AS Id,
                created_at AS CreatedAt,
                created_by_user_id AS CreatedByUserId,
                modified_at AS ModifiedAt,
                modified_by_user_id AS ModifiedByUserId,
                playback_speed AS PlaybackSpeed
         FROM media.preferences
         """;

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
                    COALESCE(channel_p.playback_speed, library_p.playback_speed) AS PlaybackSpeed
             FROM media.channels
                      LEFT OUTER JOIN media.channel_preferences ON channels.id = channel_preferences.channel_id
                      LEFT OUTER JOIN media.preferences channel_p ON channel_preferences.preference_id = channel_p.id

                      INNER JOIN media.libraries ON libraries.id = @{nameof(libraryId)}
                      LEFT OUTER JOIN media.library_preferences ON libraries.id = library_preferences.library_id
                      LEFT OUTER JOIN media.preferences library_p ON library_preferences.preference_id = library_p.id
             WHERE channels.id = @{nameof(channelId)}
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
                    COALESCE(channel_p.playback_speed, library_p.playback_speed) AS PlaybackSpeed
             FROM media.videos
                      INNER JOIN media.channels ON videos.channel_id = channels.id
                      LEFT OUTER JOIN media.channel_preferences ON channels.id = channel_preferences.channel_id
                      LEFT OUTER JOIN media.preferences channel_p ON channel_preferences.preference_id = channel_p.id

                      INNER JOIN media.libraries ON libraries.id = @{nameof(libraryId)}
                      LEFT OUTER JOIN media.library_preferences ON libraries.id = library_preferences.library_id
                      LEFT OUTER JOIN media.preferences library_p ON library_preferences.preference_id = library_p.id
             WHERE videos.id = @{nameof(videoId)}
             """,
            new { libraryId, videoId, userId },
            cancellationToken: cancellationToken));
    }
}
