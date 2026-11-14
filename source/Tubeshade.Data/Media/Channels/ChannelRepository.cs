using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Channels;

public sealed partial class ChannelRepository(NpgsqlConnection connection) : ModifiableRepositoryBase<ChannelEntity>(connection)
{
    protected override string InsertSql => Queries.Insert;

    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;

    public async ValueTask<List<ChannelEntity>> GetSubscribedForLibrary(
        Guid libraryId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            Queries.GetSubscribedForLibrary,
            new GetFromLibraryParameters(userId, libraryId, Permissions.Read),
            transaction);

        var enumerable = await Connection.QueryAsync<ChannelEntity>(command);
        return enumerable as List<ChannelEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<ChannelEntity>> GetForLibrary(
        Guid libraryId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.GetForLibrary,
            new GetFromLibraryParameters(userId, libraryId, Permissions.Read),
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<ChannelEntity>(command);
        return enumerable as List<ChannelEntity> ?? enumerable.ToList();
    }

    public async ValueTask<ChannelEntity?> FindByExternalId(
        string externalId,
        Guid userId,
        Permissions permissions,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            Queries.FindByExternalId,
            new GetSingleExternalParameters(externalId, userId, permissions | Permissions.Read),
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<ChannelEntity>(command);
    }

    /// <summary>Adds the channel to the library, if the user holds Update on the library and Read on the channel.</summary>
    public async ValueTask<int> AddToLibrary(Guid libraryId, Guid channelId, Guid userId, NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            Queries.AddToLibrary,
            Access.Parameters(new UserPrincipal(userId), Resources.Library, Permissions.Update, new { libraryId, channelId, LibraryPermissions = Permissions.Update, ChannelPermissions = Permissions.Read }),
            transaction);
    }

    public async ValueTask<int> MoveToLibrary(
        Guid newLibraryId,
        Guid channelId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            new CommandDefinition(
                Queries.MoveToLibrary,
                Access.Parameters(new UserPrincipal(userId), Resources.Library, Permissions.Update, new { newLibraryId, channelId }),
                transaction));
    }

    public async ValueTask<Guid> GetPrimaryLibraryId(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.GetPrimaryLibraryId,
            new GetSingleParameters(id, userId, Permissions.Read),
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleAsync<Guid>(command);
    }

    public async ValueTask<Guid> GetPrimaryLibraryId(
        Guid id,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.GetPrimaryLibraryId,
            new GetSingleParameters(id, userId, Permissions.Read),
            transaction,
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleAsync<Guid>(command);
    }

    public async ValueTask<Guid> GetPrimaryLibraryIdForSystemAsync(
        SystemPrincipal system,
        Guid id,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.GetPrimaryLibraryIdForSystem,
            new { id },
            transaction,
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleAsync<Guid>(command);
    }

    public async ValueTask<List<DetailedChannel>> GetFiltered(
        ChannelParameters parameters,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(Queries.GetFiltered, parameters, transaction, cancellationToken: cancellationToken);
        return await GetFiltered(command);
    }

    public async ValueTask<DetailedChannel> GetDetailed(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new ChannelParameters
        {
            Limit = 2,
            Offset = 0,
            UserId = userId,
            Id = id,
            SortBy = SortChannelBy.ChannelName,
            SortDirection = SortDirection.Ascending,
        };

        var command = new CommandDefinition(Queries.GetFiltered, parameters, cancellationToken: cancellationToken);
        var channels = await GetFiltered(command);
        return channels.Single();
    }

    private async ValueTask<List<DetailedChannel>> GetFiltered(CommandDefinition command)
    {
        var enumerable = await Connection.QueryAsync<DetailedChannel, ImageFileEntity?, DetailedChannel>(command, MapSplitRow);

        return enumerable
            .GroupBy(channel => channel.Id)
            .Select(grouping =>
            {
                var images = grouping.SelectMany(channel => channel.Images).ToArray();
                var channel = grouping.First();

                channel.Banners = images.Where(image => image.Type == ImageType.Banner).ToArray();
                channel.Thumbnails = images.Where(image => image.Type == ImageType.Thumbnail).ToArray();
                return channel;
            })
            .ToList();
    }

    private static DetailedChannel MapSplitRow(DetailedChannel channel, ImageFileEntity? image)
    {
        if (image is not null)
        {
            channel.Images = [image];
        }

        return channel;
    }
}
