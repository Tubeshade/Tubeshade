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

public sealed class PlaylistRepository : ModifiableRepositoryBase<PlaylistEntity>
{
    public PlaylistRepository(NpgsqlConnection connection)
        : base(connection)
    {
    }

    /// <inheritdoc />
    protected override string TableName => "media.playlists";

    /// <inheritdoc />
    protected override string InsertSql =>
        """
        INSERT INTO media.playlists (created_by_user_id, modified_by_user_id, library_id, storage_path, external_id, external_url, name, refreshed_at, subscribed_at)
        VALUES (@CreatedByUserId, @ModifiedByUserId, @LibraryId, @StoragePath, @ExternalId, @ExternalUrl, @Name, @RefreshedAt, @SubscribedAt)
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
               library_id,
               storage_path,
               external_id,
               external_url,
               name,
               refreshed_at,
               subscribed_at
        FROM media.playlists
        """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        """
        library_id = @LibraryId,
        storage_path = @StoragePath,
        external_id = @ExternalId,
        external_url = @ExternalUrl,
        name = @Name,
        refreshed_at = @RefreshedAt,
        subscribed_at = @SubscribedAt
        """;

    /// <inheritdoc />
    /// <remarks>Playlists have no owner of their own - access is inherited from the library that contains them.</remarks>
    protected override string AccessCte => GetAccessCte(nameof(GetParameters.UserId), nameof(GetParameters.Access));

    /// <inheritdoc />
    /// <remarks>Playlists have no owner of their own - access is inherited from the library that contains them.</remarks>
    protected override string UpdateAccessCte =>
        GetAccessCte(nameof(IModifiableEntity.ModifiedByUserId), nameof(IModifiableEntity.AccessFoo));

    public async ValueTask<PlaylistEntity?> FindByExternalId(
        string externalId,
        Guid userId,
        Access access,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            $"""
             {SelectAccessibleSql}
               AND {TableName}.external_id = @{nameof(GetSingleExternalParameters.ExternalId)};
             """,
            new GetSingleExternalParameters(externalId, userId, access),
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<PlaylistEntity>(command);
    }

    public async ValueTask<List<PlaylistEntity>> GetSubscribedForLibrary(
        Guid libraryId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             {SelectAccessibleSql}
                AND playlists.library_id = @{nameof(GetFromLibraryParameters.LibraryId)}
                AND playlists.subscribed_at IS NOT NULL;
             """,
            new GetFromLibraryParameters(userId, libraryId, Access.Read),
            transaction);

        var enumerable = await Connection.QueryAsync<PlaylistEntity>(command);
        return enumerable as List<PlaylistEntity> ?? enumerable.ToList();
    }

    /// <summary>Gets the playlists that contain the given video, ordered by name.</summary>
    public async ValueTask<List<PlaylistEntity>> GetForVideo(
        Guid videoId,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new GetVideoParameters(videoId, userId, Access.Read);

        var command = new CommandDefinition(
            // lang=sql
            $"""
             {SelectAccessibleSql}
                AND EXISTS(SELECT 1
                           FROM media.playlist_videos
                           WHERE playlist_videos.playlist_id = playlists.id
                             AND playlist_videos.video_id = @{nameof(parameters.VideoId)})
             ORDER BY playlists.name;
             """,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<PlaylistEntity>(command);
        return enumerable as List<PlaylistEntity> ?? enumerable.ToList();
    }

    /// <summary>Replaces all entries of the playlist with <paramref name="videoIds"/>, in the given order.</summary>
    public async ValueTask ReplaceVideos(
        Guid playlistId,
        Guid[] videoIds,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             DELETE FROM media.playlist_videos
             WHERE playlist_id = @{nameof(playlistId)}
               AND NOT (video_id = ANY(@{nameof(videoIds)}));

             INSERT INTO media.playlist_videos (playlist_id, video_id, "order")
             SELECT @{nameof(playlistId)}, video_id, ordinality - 1
             FROM UNNEST(@{nameof(videoIds)}) WITH ORDINALITY AS entries(video_id, ordinality)
             ON CONFLICT (playlist_id, video_id) DO UPDATE SET "order" = excluded."order";
             """,
            new { playlistId, videoIds },
            transaction,
            cancellationToken: cancellationToken);

        await Connection.ExecuteAsync(command);
    }

    public async ValueTask<List<DetailedPlaylist>> GetFiltered(
        PlaylistParameters parameters,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            GetFilteredQuery(parameters),
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

    private static string GetAccessCte(string userIdParameter, string accessParameter) =>
        // lang=sql
        $"""
         WITH accessible AS
         (SELECT playlists.id
          FROM media.playlists
                   INNER JOIN media.libraries ON libraries.id = playlists.library_id
                   INNER JOIN identity.owners ON owners.id = libraries.owner_id
                   INNER JOIN identity.ownerships ON
              ownerships.owner_id = owners.id AND
              ownerships.user_id = @{userIdParameter} AND
              (ownerships.access = @{accessParameter} OR ownerships.access = 'owner'))
         """;

    private static string GetFilteredQuery(PlaylistParameters parameters) =>
        // lang=sql
        $"""
         WITH accessible AS
             (SELECT playlists.id
              FROM media.playlists
                  INNER JOIN media.libraries ON libraries.id = playlists.library_id
                  INNER JOIN identity.owners ON owners.id = libraries.owner_id
                  INNER JOIN identity.ownerships ON
                      ownerships.owner_id = owners.id AND
                      ownerships.user_id = @{nameof(parameters.UserId)} AND
                      (ownerships.access = @{nameof(parameters.Access)} OR ownerships.access = 'owner')),

             parameters AS MATERIALIZED (SELECT websearch_to_tsquery('english', @{nameof(parameters.Query)}) AS query),

             filtered AS (
                 SELECT id,
                        created_at,
                        created_by_user_id,
                        modified_at,
                        modified_by_user_id,
                        library_id,
                        storage_path,
                        external_id,
                        external_url,
                        name,
                        refreshed_at,
                        subscribed_at,
                        (SELECT count(*)
                         FROM media.playlist_videos
                         WHERE playlist_videos.playlist_id = playlists.id) AS video_count,
                        count(*) OVER()                                    AS total_count
                 FROM media.playlists
                     CROSS JOIN parameters
                 WHERE (playlists.id IN (SELECT id FROM accessible))
                   AND (@{nameof(parameters.LibraryId)} IS NULL OR playlists.library_id = @{nameof(parameters.LibraryId)})
                   AND (@{nameof(parameters.Query)} IS NULL OR playlists.name @@ query)
                 ORDER BY {parameters.SortBy.SortExpression} {parameters.SortDirection.Name} NULLS LAST, playlists.id
                 LIMIT @{nameof(parameters.Limit)}
                 OFFSET @{nameof(parameters.Offset)})

         SELECT playlists.id,
                playlists.created_at,
                playlists.created_by_user_id,
                playlists.modified_at,
                playlists.modified_by_user_id,
                playlists.library_id,
                playlists.storage_path,
                playlists.external_id,
                playlists.external_url,
                playlists.name,
                playlists.refreshed_at,
                playlists.subscribed_at,
                playlists.video_count,
                playlists.total_count,

                image_files.id,
                image_files.created_at,
                image_files.created_by_user_id,
                image_files.modified_at,
                image_files.modified_by_user_id,
                image_files.storage_path,
                image_files.type,
                image_files.width,
                image_files.height,
                image_files.hash_algorithm,
                image_files.hash,
                image_files.storage_size
         FROM filtered playlists
             LEFT JOIN media.playlist_images ON playlist_images.playlist_id = playlists.id
             LEFT JOIN media.image_files ON playlist_images.image_id = image_files.id
         ORDER BY {parameters.SortBy.SortExpression} {parameters.SortDirection.Name} NULLS LAST, playlists.id;
         """;

    private static DetailedPlaylist MapSplitRow(DetailedPlaylist playlist, ImageFileEntity? image)
    {
        if (image is not null)
        {
            playlist.Thumbnails = [image];
        }

        return playlist;
    }
}
