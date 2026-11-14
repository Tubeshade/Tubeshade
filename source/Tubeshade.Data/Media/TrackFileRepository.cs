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

public sealed partial class TrackFileRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<TrackFileEntity>(connection)
{
    /// <remarks>Added by users who can update the video.</remarks>
    protected override string InsertSql => Queries.Insert;

    protected override Permissions InsertPermissions => Permissions.Update;

    /// <remarks>Track files have no owner of their own - access is inherited from the video's primary library.</remarks>
    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;

    public async ValueTask<List<TrackFileEntity>> GetForVideo(
        Guid videoId,
        long userId,
        Permissions permissions,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new GetVideoParameters(videoId, userId, permissions | Permissions.Read);

        var command = new CommandDefinition(
            Queries.GetForVideo,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<TrackFileEntity>(command);
        return enumerable as List<TrackFileEntity> ?? enumerable.ToList();
    }
}
