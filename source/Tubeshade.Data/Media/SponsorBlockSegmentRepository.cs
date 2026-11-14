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

public sealed partial class SponsorBlockSegmentRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<SponsorBlockSegmentEntity>(connection)
{
    /// <remarks>Added by users who can update the video.</remarks>
    protected override string InsertSql => Queries.Insert;

    protected override Permissions InsertPermissions => Permissions.Update;

    /// <remarks>Segments have no owner of their own - access is inherited from the video.</remarks>
    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;

    public async ValueTask<List<SponsorBlockSegmentEntity>> GetForVideo(
        Guid videoId,
        long userId,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            Queries.GetForVideo,
            new GetVideoParameters(videoId, userId, Permissions.Read),
            transaction);

        var enumerable = await Connection.QueryAsync<SponsorBlockSegmentEntity>(command);
        return enumerable as List<SponsorBlockSegmentEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<SponsorBlockSegmentEntity>> GetForVideos(
        Guid[] videoIds,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.GetForVideos,
            new GetVideosParameters(videoIds, userId, Permissions.Read),
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<SponsorBlockSegmentEntity>(command);
        return enumerable as List<SponsorBlockSegmentEntity> ?? enumerable.ToList();
    }
}
