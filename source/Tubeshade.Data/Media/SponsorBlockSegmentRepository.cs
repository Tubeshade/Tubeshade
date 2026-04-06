using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media;

public sealed class SponsorBlockSegmentRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<SponsorBlockSegmentEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.sponsorblock_segments";

    /// <inheritdoc />
    protected override string InsertSql =>
        """
        INSERT INTO media.sponsorblock_segments (created_by_user_id, modified_by_user_id, video_id, external_id, start_time, end_time, category, action, locked, description) 
        VALUES (@CreatedByUserId, @ModifiedByUserId, @VideoId, @ExternalId, @StartTime, @EndTime, @Category, @Action, @Locked, @Description)
        RETURNING id;
        """;

    /// <inheritdoc />
    protected override string SelectSql =>
        """
        SELECT id,
               created_at,
               created_by_user_id,
               modified_at,
               modified_by_user_id,
               video_id,
               external_id,
               start_time,
               end_time,
               category,
               action,
               locked,
               description
        FROM media.sponsorblock_segments
        """;

    /// <inheritdoc />
    protected override string UpdateSet => throw new NotSupportedException();

    /// <inheritdoc />
    protected override string UpdateSql =>
        $"""
         UPDATE media.sponsorblock_segments
         SET modified_at = CURRENT_TIMESTAMP,
             modified_by_user_id = @{nameof(SponsorBlockSegmentEntity.ModifiedByUserId)},
             start_time = @{nameof(SponsorBlockSegmentEntity.StartTime)},
             end_time = @{nameof(SponsorBlockSegmentEntity.EndTime)},
             category = @{nameof(SponsorBlockSegmentEntity.Category)},
             action = @{nameof(SponsorBlockSegmentEntity.Action)},
             locked = @{nameof(SponsorBlockSegmentEntity.Locked)},
             description = @{nameof(SponsorBlockSegmentEntity.Description)}

         WHERE (sponsorblock_segments.id = @{nameof(SponsorBlockSegmentEntity.Id)});
         """;

    /// <inheritdoc />
    protected override string DeleteSql =>
        $"""
         DELETE FROM media.sponsorblock_segments
         WHERE sponsorblock_segments.id = @{nameof(GetSingleParameters.Id)};
         """;

    public async ValueTask<List<SponsorBlockSegmentEntity>> GetForVideo(
        Guid videoId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            $"""
             {SelectSql}
             WHERE video_id = @{nameof(GetVideoParameters.VideoId)};
             """,
            new GetVideoParameters(videoId, userId, Access.Read),
            transaction);

        var enumerable = await Connection.QueryAsync<SponsorBlockSegmentEntity>(command);
        return enumerable as List<SponsorBlockSegmentEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<SponsorBlockSegmentEntity>> GetForVideo(
        Guid videoId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            $"""
             {SelectSql}
             WHERE video_id = @{nameof(GetVideoParameters.VideoId)};
             """,
            new GetVideoParameters(videoId, userId, Access.Read),
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<SponsorBlockSegmentEntity>(command);
        return enumerable as List<SponsorBlockSegmentEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<SponsorBlockSegmentEntity>> GetForVideos(
        Guid[] videoIds,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            $"""
             {SelectSql}
             WHERE video_id = ANY (@{nameof(GetVideosParameters.VideoIds)});
             """,
            new GetVideosParameters(videoIds, userId, Access.Read),
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<SponsorBlockSegmentEntity>(command);
        return enumerable as List<SponsorBlockSegmentEntity> ?? enumerable.ToList();
    }
}
