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

/// <remarks>
/// Images have no owner of their own: access is inherited from the primary library of the video, channel or playlist
/// that the image belongs to.
/// </remarks>
public sealed partial class ImageFileRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<ImageFileEntity>(connection)
{
    /// <remarks>An image is only reachable once it is linked to a video, channel or playlist.</remarks>
    protected override string InsertSql => Queries.Insert;

    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;

    /// <summary>Links the image to the video, if the user holds Update on the video.</summary>
    public async ValueTask<int> LinkToVideoAsync(Guid id, Guid videoId, Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            Queries.LinkToVideo,
            Access.Parameters(new UserPrincipal(userId), Resources.Video, Permissions.Update, new { id, videoId }),
            transaction);

        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask<List<ImageFileEntity>> GetForVideo(
        Guid videoId,
        Guid userId,
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

        var enumerable = await Connection.QueryAsync<ImageFileEntity>(command);
        return enumerable as List<ImageFileEntity> ?? enumerable.ToList();
    }

    /// <summary>Links the image to the channel, if the user holds Update on the channel.</summary>
    public async ValueTask<int> LinkToChannelAsync(Guid id, Guid channelId, Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            Queries.LinkToChannel,
            Access.Parameters(new UserPrincipal(userId), Resources.Channel, Permissions.Update, new { id, channelId }),
            transaction);

        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask<List<ImageFileEntity>> GetForChannel(
        Guid channelId,
        Guid userId,
        Permissions permissions,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new GetVideoParameters(channelId, userId, permissions | Permissions.Read);

        var command = new CommandDefinition(
            Queries.GetForChannel,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<ImageFileEntity>(command);
        return enumerable as List<ImageFileEntity> ?? enumerable.ToList();
    }

    /// <summary>Links the image to the playlist, if the user holds Update on the playlist's library.</summary>
    public async ValueTask<int> LinkToPlaylistAsync(Guid id, Guid playlistId, Guid userId, NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            Queries.LinkToPlaylist,
            Access.Parameters(new UserPrincipal(userId), Resources.Playlist, Permissions.Update, new { id, playlistId }),
            transaction);

        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask<List<ImageFileEntity>> GetForPlaylist(
        Guid playlistId,
        Guid userId,
        Permissions permissions,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new GetVideoParameters(playlistId, userId, permissions | Permissions.Read);

        var command = new CommandDefinition(
            Queries.GetForPlaylist,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<ImageFileEntity>(command);
        return enumerable as List<ImageFileEntity> ?? enumerable.ToList();
    }

    /// <summary>Gets the storage path of the video, channel or playlist that the image belongs to, if the user can read it.</summary>
    public async ValueTask<string?> GetPath(
        Guid id,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            Queries.GetPath,
            new GetSingleParameters(id, userId, Permissions.Read),
            transaction,
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleOrDefaultAsync<string>(command);
    }
}
