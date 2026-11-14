using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Creators;

public sealed partial class CreatorRepository : ModifiableRepositoryBase<CreatorEntity>
{
    /// <inheritdoc />
    public CreatorRepository(NpgsqlConnection connection)
        : base(connection)
    {
    }

    protected override string InsertSql => Queries.Insert;

    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;

    public async ValueTask<List<CreatorChannel>> GetChannels(
        Guid creatorId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.GetChannels,
            Access.Parameters(new UserPrincipal(userId), Resources.Creator, Permissions.Read, new { creatorId }),
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<CreatorChannel>(command);
        return enumerable.AsList();
    }

    public async ValueTask UpdateChannels(
        Guid creatorId,
        Guid primaryChannelId,
        Guid[] channelIds,
        long userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.UpdateChannels,
            Access.Parameters(new UserPrincipal(userId), Resources.Creator, Permissions.Update, new { creatorId, primaryChannelId, channelIds, CreatorPermissions = Permissions.Update, LibraryPermissions = Permissions.Read }),
            transaction,
            cancellationToken: cancellationToken);

        await Connection.ExecuteAsync(command);
    }

    public async ValueTask<List<DetailedCreator>> GetFiltered(
        CreatorParameters parameters,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(Queries.GetFiltered, parameters, transaction, cancellationToken: cancellationToken);
        return await GetFiltered(command);
    }

    private async ValueTask<List<DetailedCreator>> GetFiltered(CommandDefinition command)
    {
        var enumerable = await Connection.QueryAsync<DetailedCreator, ImageFileEntity?, DetailedCreator>(command, MapSplitRow);

        return enumerable
            .GroupBy(video => video.Id)
            .Select(grouping =>
            {
                var video = grouping.First();
                video.Thumbnails = grouping.SelectMany(channel => channel.Thumbnails).ToArray();
                return video;
            })
            .ToList();
    }

    private static DetailedCreator MapSplitRow(DetailedCreator video, ImageFileEntity? image)
    {
        if (image is not null)
        {
            video.Thumbnails = [image];
        }

        return video;
    }
}
