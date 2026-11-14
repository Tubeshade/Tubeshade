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
        $"""
         INSERT INTO media.sponsorblock_segments (created_by_user_id, modified_by_user_id, video_id, external_id, start_time, end_time, category, action, locked, description)
         SELECT @CreatedByUserId, @ModifiedByUserId, @VideoId, @ExternalId, @StartTime, @EndTime, @Category, @Action, @Locked, @Description
         WHERE @{nameof(SponsorBlockSegmentEntity.VideoId)} IN (SELECT videos.id
                                                               FROM media.videos
                                                               WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@{nameof(IEntity.CreatedByUserId)}, {(int)Permissions.Update})))
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
    protected override string UpdateSet =>
        $"""
         start_time = @{nameof(SponsorBlockSegmentEntity.StartTime)},
         end_time = @{nameof(SponsorBlockSegmentEntity.EndTime)},
         category = @{nameof(SponsorBlockSegmentEntity.Category)},
         action = @{nameof(SponsorBlockSegmentEntity.Action)},
         locked = @{nameof(SponsorBlockSegmentEntity.Locked)},
         description = @{nameof(SponsorBlockSegmentEntity.Description)}
         """;

    /// <inheritdoc />
    /// <remarks>Segments have no owner of their own - access is inherited from the video.</remarks>
    protected override string AccessFilter =>
        $"(sponsorblock_segments.video_id IN (SELECT videos.id FROM media.videos WHERE videos.owner_id IN ({Access.AccessibleOwners})))";

    protected override string UpdateAccessFilter =>
        $"(sponsorblock_segments.video_id IN (SELECT videos.id FROM media.videos WHERE videos.owner_id IN ({UpdatableOwners})))";

    public async ValueTask<List<SponsorBlockSegmentEntity>> GetForVideo(
        Guid videoId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            $"""
             {SelectSql}
             WHERE video_id = @{nameof(GetVideoParameters.VideoId)}
               AND {AccessFilter};
             """,
            new GetVideoParameters(videoId, userId, Permissions.Read),
            transaction);

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
             WHERE video_id = ANY (@{nameof(GetVideosParameters.VideoIds)})
               AND {AccessFilter};
             """,
            new GetVideosParameters(videoIds, userId, Permissions.Read),
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<SponsorBlockSegmentEntity>(command);
        return enumerable as List<SponsorBlockSegmentEntity> ?? enumerable.ToList();
    }
}
