using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Videos;

public sealed partial class VideoRepository(NpgsqlConnection connection) : ModifiableRepositoryBase<VideoEntity>(connection)
{
    /// <remarks>A video takes the owner of its channel, so <see cref="VideoEntity.OwnerId"/> is not used.</remarks>
    protected override string InsertSql => Queries.Insert;

    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;

    public async ValueTask<List<DetailedVideo>> GetDownloadableVideos(
        VideoParameters parameters,
        CancellationToken cancellationToken = default)
    {
        parameters.Downloadable = true;

        var command = new CommandDefinition(Queries.GetFilteredDetailed, parameters, cancellationToken: cancellationToken);
        return await GetDetailed(command);
    }

    public async ValueTask<List<DetailedVideo>> GetFilteredDetailed(
        VideoParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(Queries.GetFilteredDetailed, parameters, cancellationToken: cancellationToken);
        return await GetDetailed(command);
    }

    public async ValueTask<List<VideoEntity>> GetFiltered(
        VideoParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(Queries.GetFiltered, parameters, cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoEntity>(command);
        return enumerable as List<VideoEntity> ?? enumerable.ToList();
    }

    public async ValueTask<VideoEntity?> GetLatestDownloadedVideo(
        long userId,
        Guid libraryId,
        Guid channelId,
        NpgsqlTransaction transaction)
    {
        var parameters = new VideoParameters
        {
            UserId = userId,
            LibraryId = libraryId,
            ChannelId = channelId,
            Limit = 1,
            Offset = 0,
            WithFiles = true,
            SortBy = SortVideoBy.PublishedAt,
            SortDirection = SortDirection.Descending,
        };

        var command = new CommandDefinition(Queries.GetFiltered, parameters, transaction);
        return await Connection.QuerySingleOrDefaultAsync<VideoEntity>(command);
    }

    public async ValueTask<List<(Guid VideoId, Guid ChannelId, string VideoUrl)>> GetForReindex(
        Guid libraryId,
        long userId,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            Queries.GetForReindex,
            new GetFromLibraryParameters(userId, libraryId, Permissions.Read),
            transaction);

        var enumerable = await Connection.QueryAsync<(Guid, Guid, string)>(command);
        return enumerable as List<(Guid, Guid, string)> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoFileEntity>> GetFilesAsync(
        Guid videoId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.GetFiles,
            new GetVideoParameters(videoId, userId, Permissions.Read),
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoFileEntity>(command);
        return enumerable as List<VideoFileEntity> ?? enumerable.ToList();
    }

    public async ValueTask<List<VideoFileEntity>> GetFilesAsync(
        Guid videoId,
        long userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.GetFiles,
            new GetVideoParameters(videoId, userId, Permissions.Read),
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<VideoFileEntity>(command);
        return enumerable as List<VideoFileEntity> ?? enumerable.ToList();
    }

    public async ValueTask<VideoFileEntity?> FindFileAsync(
        Guid id,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.FindFile,
            new GetSingleParameters(id, userId, Permissions.Read),
            cancellationToken: cancellationToken);

        return await Connection.QuerySingleOrDefaultAsync<VideoFileEntity>(command);
    }

    public async ValueTask<VideoEntity?> FindByExternalId(
        string externalId,
        long userId,
        Permissions permissions,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            Queries.FindByExternalId,
            new GetSingleExternalParameters(externalId, userId, permissions | Permissions.Read),
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<VideoEntity>(command);
    }

    public async ValueTask<List<EntityId>> FindByExternalIds(
        List<string> externalIds,
        long userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.FindByExternalIds,
            Access.Parameters(new UserPrincipal(userId), Resources.Video, Permissions.Read, new { externalIds }),
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<EntityId>(command);
        return enumerable as List<EntityId> ?? enumerable.ToList();
    }

    public async ValueTask<VideoEntity?> FindByExternalUrl(string externalUrl, long userId, Permissions permissions,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            Queries.FindByExternalUrl,
            new GetSingleExternalUrlParameters(externalUrl, userId, permissions | Permissions.Read),
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<VideoEntity>(command);
    }

    public async ValueTask<List<EntityId>> GetWithoutSegments(
        long userId,
        Guid libraryId,
        NpgsqlTransaction transaction)
    {
        var enumerable = await Connection.QueryAsync<EntityId>(
            Queries.GetWithoutSegments,
            new GetFromLibraryParameters(userId, libraryId, Permissions.Read),
            transaction);

        return enumerable as List<EntityId> ?? enumerable.ToList();
    }

    public async ValueTask<List<EntityId>> GetWithUnlockedSegments(
        long userId,
        Guid libraryId,
        NpgsqlTransaction transaction)
    {
        var enumerable = await Connection.QueryAsync<EntityId>(
            Queries.GetWithUnlockedSegments,
            new GetFromLibraryParameters(userId, libraryId, Permissions.Read),
            transaction);

        return enumerable as List<EntityId> ?? enumerable.ToList();
    }

    public async ValueTask<int> SetWatched(Guid videoId, long userId, bool viewed, NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            Queries.SetWatched,
            Access.Parameters(new UserPrincipal(userId), Resources.Video, Permissions.Read, new { VideoId = videoId, viewed }),
            transaction);
    }

    public async ValueTask<int> SavePlaybackActivity(Guid videoId, long userId, double position, double watchTime, double playbackTime, NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            Queries.SavePlaybackActivity,
            Access.Parameters(new UserPrincipal(userId), Resources.Video, Permissions.Read, new { VideoId = videoId, position, watchTime, playbackTime }),
            transaction);
    }

    public async ValueTask<int> AddPlay(Guid videoId, long userId, NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            Queries.AddPlay,
            new GetVideoParameters(videoId, userId, Permissions.Read),
            transaction);
    }

    public async ValueTask<VideoPlaybackStatistics> GetPlaybackStatistics(Guid videoId, long userId,
        NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleAsync<VideoPlaybackStatistics>(
            Queries.GetPlaybackStatistics,
            new { videoId, userId },
            transaction);
    }

    private async ValueTask<List<DetailedVideo>> GetDetailed(CommandDefinition command)
    {
        var enumerable = await Connection.QueryAsync<DetailedVideo, ImageFileEntity?, DetailedVideo>(command, MapSplitRow);

        return enumerable
            .GroupBy(video => (video.Id, video.PlaylistOrder))
            .Select(grouping =>
            {
                var video = grouping.First();
                video.Thumbnails = grouping.SelectMany(channel => channel.Thumbnails).ToArray();
                return video;
            })
            .ToList();
    }

    private static DetailedVideo MapSplitRow(DetailedVideo video, ImageFileEntity? image)
    {
        if (image is not null)
        {
            video.Thumbnails = [image];
        }

        return video;
    }
}
