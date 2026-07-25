using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media.Creators;

public sealed class CreatorRepository : ModifiableRepositoryBase<CreatorEntity>
{
    /// <inheritdoc />
    public CreatorRepository(NpgsqlConnection connection)
        : base(connection)
    {
    }

    /// <inheritdoc />
    protected override string TableName => "media.creators";

    /// <inheritdoc />
    protected override string InsertSql =>
        """
        INSERT INTO media.creators (created_by_user_id, modified_by_user_id, owner_id, name)
        VALUES (@CreatedByUserId, @ModifiedByUserId, @OwnerId, @Name)
        RETURNING id;
        """;

    /// <inheritdoc />
    protected override string SelectSql =>
        """
        SELECT id, created_at, created_by_user_id, modified_at, modified_by_user_id, owner_id, name
        FROM media.creators
        """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        """
        name = @Name
        """;

    public async ValueTask<List<CreatorChannel>> GetChannels(
        Guid creatorId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            $"""
             SELECT channel_id,
                    "primary"
             FROM media.creator_channels
             WHERE creator_id = @{nameof(creatorId)}
             """,
            new { creatorId, userId },
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<CreatorChannel>(command);
        return enumerable.AsList();
    }

    public async ValueTask UpdateChannels(
        Guid creatorId,
        Guid primaryChannelId,
        Guid[] channelIds,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             WITH libs AS (SELECT libraries.id
                           FROM media.libraries
                               INNER JOIN identity.owners ON owners.id = media.libraries.owner_id
                               INNER JOIN identity.ownerships ON
                                   ownerships.owner_id = owners.id AND
                                   ownerships.user_id = @{nameof(userId)} AND
                                   (ownerships.access = 'read' OR ownerships.access = 'owner')),
                 accessible AS (SELECT channel_id
                                FROM media.library_channels
                                WHERE EXISTS(SELECT 1 FROM libs WHERE libs.id = library_id))
             

             DELETE
             FROM media.creator_channels
             WHERE creator_id = @{nameof(creatorId)}
               AND EXISTS(SELECT 1 FROM accessible WHERE accessible.channel_id = creator_channels.channel_id);

             WITH libs AS (SELECT libraries.id
                           FROM media.libraries
                               INNER JOIN identity.owners ON owners.id = media.libraries.owner_id
                               INNER JOIN identity.ownerships ON
                                   ownerships.owner_id = owners.id AND
                                   ownerships.user_id = @{nameof(userId)} AND
                                   (ownerships.access = 'read' OR ownerships.access = 'owner')),
                 accessible AS (SELECT channel_id
                                FROM media.library_channels
                                WHERE EXISTS(SELECT 1 FROM libs WHERE libs.id = library_id)),
                 insertable AS (SELECT @{nameof(creatorId)} AS creator_id,
                                       channel_id,
                                       false AS "primary"
                                FROM UNNEST(@{nameof(channelIds)}) AS t(channel_id)
                                WHERE EXISTS(SELECT 1 from accessible WHERE accessible.channel_id = t.channel_id))

             INSERT
             INTO media.creator_channels (creator_id, channel_id, "primary")
             SELECT * 
             FROM insertable;
             
             WITH libs AS (SELECT libraries.id
                           FROM media.libraries
                               INNER JOIN identity.owners ON owners.id = media.libraries.owner_id
                               INNER JOIN identity.ownerships ON
                                   ownerships.owner_id = owners.id AND
                                   ownerships.user_id = @{nameof(userId)} AND
                                   (ownerships.access = 'read' OR ownerships.access = 'owner')),
                 accessible AS (SELECT channel_id
                                FROM media.library_channels
                                WHERE EXISTS(SELECT 1 FROM libs WHERE libs.id = library_id)),
                 insertable AS (SELECT @{nameof(creatorId)} AS creator_id,
                                       @{nameof(primaryChannelId)} AS channel_id,
                                       true AS "primary")

             INSERT
             INTO media.creator_channels (creator_id, channel_id, "primary")
             SELECT * 
             FROM insertable
             WHERE EXISTS(SELECT 1 FROM accessible WHERE accessible.channel_id = insertable.channel_id);
             """,
            new { creatorId, primaryChannelId, channelIds, userId },
            transaction,
            cancellationToken: cancellationToken);

        await Connection.ExecuteAsync(command);
    }

    public async ValueTask<List<DetailedCreator>> GetFiltered(
        CreatorParameters parameters,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var query = GetFilteredQuery(parameters);
        var command = new CommandDefinition(query, parameters, transaction, cancellationToken: cancellationToken);
        return await GetFiltered(command);
    }

    private string GetFilteredQuery(CreatorParameters parameters) =>
        // lang=sql
        $"""
         WITH accessible AS
             (SELECT creators.id
              FROM media.creators
                  INNER JOIN identity.owners ON owners.id = creators.owner_id
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
                        owner_id,
                        name,
                        (SELECT count(*) 
                         FROM media.creator_channels
                         WHERE creator_channels.creator_id = creators.id) AS channel_count,
                        count(*) OVER()                                   AS total_count
                 FROM media.creators
                     CROSS JOIN parameters
                 WHERE (creators.id IN (SELECT id FROM accessible))
                   AND (@{nameof(parameters.LibraryId)} IS NULL OR EXISTS(SELECT 1
                                                                         FROM media.library_channels
                                                                             INNER JOIN media.creator_channels ON library_channels.channel_id = creator_channels.channel_id
                                                                         WHERE library_id = @{nameof(parameters.LibraryId)} AND creator_id = creators.id))
                   AND (@{nameof(parameters.Query)} IS NULL OR creators.name @@ query)
                 ORDER BY {parameters.SortBy.SortExpression} {parameters.SortDirection.Name} NULLS LAST, creators.id
                 LIMIT @{nameof(parameters.Limit)}
                 OFFSET @{nameof(parameters.Offset)})

         SELECT creators.id,
                creators.created_at,
                creators.created_by_user_id,
                creators.modified_at,
                creators.modified_by_user_id,
                creators.owner_id,
                creators.name,
                creators.channel_count,
                creators.total_count,
                
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
         FROM filtered creators
             LEFT JOIN media.creator_channels ON creator_channels.creator_id = creators.id AND creator_channels."primary"
             LEFT JOIN media.channel_images ON creator_channels.channel_id = channel_images.channel_id
             LEFT JOIN media.image_files ON channel_images.image_id = image_files.id
         ORDER BY {parameters.SortBy.SortExpression} {parameters.SortDirection.Name} NULLS LAST, creators.id;
         """;

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
