using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Playlists;

public sealed partial class PlaylistRepository : ModifiableRepositoryBase<PlaylistEntity>
{
    public PlaylistRepository(NpgsqlConnection connection)
        : base(connection)
    {
    }

    protected override string InsertSql => Queries.Insert;

    /// <remarks>Playlists have no owner of their own - access is inherited from the library that contains them.</remarks>
    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;

    /// <remarks>Playlist identity is only unique within a library, so the library must be specified.</remarks>
    public async ValueTask<PlaylistEntity?> FindByExternalId(
        string externalId,
        Guid libraryId,
        Guid userId,
        Permissions permissions,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            Queries.FindByExternalId,
            new GetSingleLibraryExternalParameters(externalId, libraryId, userId, permissions | Permissions.Read),
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<PlaylistEntity>(command);
    }

    public async ValueTask<List<PlaylistEntity>> GetSubscribedForLibrary(
        Guid libraryId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            Queries.GetSubscribedForLibrary,
            new GetFromLibraryParameters(userId, libraryId, Permissions.Read),
            transaction);

        var enumerable = await Connection.QueryAsync<PlaylistEntity>(command);
        return enumerable as List<PlaylistEntity> ?? enumerable.ToList();
    }

    /// <summary>Replaces all entries of the playlist with <paramref name="videoIds"/>, in the given order.</summary>
    /// <remarks>The same video may occur multiple times in the list, mirroring playlists on external services.</remarks>
    public async ValueTask<bool> ReplaceVideos(
        Guid playlistId,
        Guid[] videoIds,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.ReplaceVideos,
            Access.Parameters(new UserPrincipal(userId), Resources.Playlist, Permissions.Update, new { playlistId, videoIds, LibraryPermissions = Permissions.Update, VideoPermissions = Permissions.Read }),
            transaction,
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleAsync<long>(command) is 1;
    }

    public async ValueTask<List<string>> GetVideoUrls(
        Guid playlistId,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.GetVideoUrls,
            Access.Parameters(new UserPrincipal(userId), Resources.Playlist, Permissions.Read, new { playlistId }),
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<string>(command);
        return enumerable.AsList();
    }

    public async ValueTask<List<DetailedPlaylist>> GetFiltered(
        PlaylistParameters parameters,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.GetFiltered,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<DetailedPlaylist, ImageFileEntity?, DetailedPlaylist>(
            command,
            MapSplitRow);

        return enumerable
            .GroupBy(playlist => playlist.Id)
            .Select(grouping =>
            {
                var playlist = grouping.First();
                playlist.Thumbnails = grouping.SelectMany(entry => entry.Thumbnails).ToArray();
                return playlist;
            })
            .ToList();
    }

    private static DetailedPlaylist MapSplitRow(DetailedPlaylist playlist, ImageFileEntity? image)
    {
        if (image is not null)
        {
            playlist.Thumbnails = [image];
        }

        return playlist;
    }
}
