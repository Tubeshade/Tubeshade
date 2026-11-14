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

public sealed class ChannelRepository(NpgsqlConnection connection) : ModifiableRepositoryBase<ChannelEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.channels";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO media.channels (created_by_user_id, modified_by_user_id, owner_id, name, storage_path, external_id, subscribed_at, subscriber_count, external_url, availability)
         SELECT @CreatedByUserId, @ModifiedByUserId, @OwnerId, @Name, @StoragePath, @ExternalId, @SubscribedAt, @SubscriberCount, @ExternalUrl, @Availability
         WHERE {CanCreateIn(nameof(ChannelEntity.OwnerId))}
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
               owner_id,
               name,
               storage_path,
               external_id,
               subscribed_at,
               subscriber_count,
               external_url,
               availability
        FROM media.channels
        """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        """
        name = @Name,
        storage_path = @StoragePath,
        external_id = @ExternalId,
        subscribed_at = @SubscribedAt,
        subscriber_count = @SubscriberCount
        """;

    public async ValueTask<List<ChannelEntity>> GetSubscribedForLibrary(
        Guid libraryId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             {SelectAccessibleSql}
                AND EXISTS(SELECT 1
                           FROM media.libraries
                               INNER JOIN media.library_channels ON libraries.id = library_channels.library_id
                           WHERE libraries.id = @{nameof(GetFromLibraryParameters.LibraryId)}
                             AND library_channels.channel_id = channels.id)
                AND channels.subscribed_at IS NOT NULL;
             """,
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
            // lang=sql
            $"""
             {SelectAccessibleSql}
                AND EXISTS(SELECT 1
                           FROM media.libraries
                               INNER JOIN media.library_channels ON libraries.id = library_channels.library_id
                           WHERE libraries.id = @{nameof(GetFromLibraryParameters.LibraryId)}
                             AND library_channels.channel_id = channels.id);
             """,
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
            $"""
             {SelectAccessibleSql}
               AND {TableName}.external_id = @{nameof(GetSingleExternalParameters.ExternalId)};
             """,
            new GetSingleExternalParameters(externalId, userId, permissions | Permissions.Read),
            transaction);

        return await Connection.QuerySingleOrDefaultAsync<ChannelEntity>(command);
    }

    /// <summary>Adds the channel to the library, if the user holds Update on the library and Read on the channel.</summary>
    public async ValueTask<int> AddToLibrary(Guid libraryId, Guid channelId, Guid userId, NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            // lang=sql
            $"""
             INSERT INTO media.library_channels (library_id, channel_id, "primary")
             SELECT libraries.id, channels.id, true
             FROM media.libraries,
                  media.channels
             WHERE libraries.id = @{nameof(libraryId)}
               AND libraries.owner_id IN ({Access.AccessibleOwners})
               AND channels.id = @{nameof(channelId)}
               AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@{nameof(IAccessParameters.PrincipalId)}, {(int)Permissions.Read}));
             """,
            Access.Parameters(new UserPrincipal(userId), Resources.Library, Permissions.Update, new { libraryId, channelId }),
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
                // lang=sql
                $"""
                 WITH accessible AS
                     (SELECT libraries.id
                      FROM media.libraries
                      WHERE libraries.owner_id IN ({Access.AccessibleOwners})
                        AND storage_path IN (SELECT storage_path FROM media.libraries WHERE id = @{nameof(newLibraryId)}))

                 UPDATE media.library_channels
                 SET library_id = @{nameof(newLibraryId)}
                 WHERE channel_id = @{nameof(channelId)} AND
                       "primary" AND
                       EXISTS(SELECT 1 FROM accessible WHERE accessible.id = library_id) AND
                       EXISTS(SELECT 1 FROM accessible WHERE accessible.id = @{nameof(newLibraryId)});
                 """,
                Access.Parameters(new UserPrincipal(userId), Resources.Library, Permissions.Update, new { newLibraryId, channelId }),
                transaction));
    }

    public async ValueTask<Guid> GetPrimaryLibraryId(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            $"""
             SELECT id
             FROM media.libraries
             INNER JOIN media.library_channels ON libraries.id = library_channels.library_id
             WHERE library_channels.channel_id = @{nameof(GetSingleParameters.Id)} AND library_channels."primary" = true
               AND libraries.owner_id IN ({Access.AccessibleOwners});
             """,
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
            $"""
             SELECT id
             FROM media.libraries
             INNER JOIN media.library_channels ON libraries.id = library_channels.library_id
             WHERE library_channels.channel_id = @{nameof(GetSingleParameters.Id)} AND library_channels."primary" = true
               AND libraries.owner_id IN ({Access.AccessibleOwners});
             """,
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
            $"""
             SELECT id
             FROM media.libraries
             INNER JOIN media.library_channels ON libraries.id = library_channels.library_id
             WHERE library_channels.channel_id = @{nameof(id)} AND library_channels."primary" = true;
             """,
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
        var query = GetFilteredChannelsQuery(parameters);
        var command = new CommandDefinition(query, parameters, transaction, cancellationToken: cancellationToken);
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

        var query = GetFilteredChannelsQuery(parameters);
        var command = new CommandDefinition(query, parameters, cancellationToken: cancellationToken);
        var channels = await GetFiltered(command);
        return channels.Single();
    }

    private string GetFilteredChannelsQuery(ChannelParameters parameters) =>
        // lang=sql
        $"""
         WITH filtered AS (
             SELECT channels.id,
                channels.created_at,
                channels.created_by_user_id,
                channels.modified_at,
                channels.modified_by_user_id,
                channels.owner_id,
                channels.name,
                channels.storage_path,
                channels.external_id,
                channels.subscribed_at,
                channels.subscriber_count,
                channels.external_url,
                channels.availability,
                (SELECT library_id
                 FROM media.library_channels
                 WHERE channel_id = channels.id AND library_channels."primary") AS primary_library_id,
                (SELECT count(*)
                 FROM media.videos
                 WHERE videos.channel_id = channels.id)                         AS video_count,
                count(*) OVER()                                                 AS total_count
             FROM media.channels
             WHERE {AccessFilter}
               AND (@{nameof(parameters.Id)} IS NULL OR channels.id = @{nameof(parameters.Id)})
               AND (@{nameof(parameters.LibraryId)} IS NULL OR EXISTS(SELECT 1 FROM media.library_channels WHERE library_id = @{nameof(parameters.LibraryId)} AND channel_id = channels.id))
               AND (@{nameof(parameters.CreatorId)} IS NULL OR EXISTS(SELECT 1 FROM media.creator_channels WHERE creator_id = @{nameof(parameters.CreatorId)} AND channel_id = channels.id))
               AND (@{nameof(parameters.Query)} IS NULL OR channels.name @@ websearch_to_tsquery('english', @{nameof(parameters.Query)}))
               AND (@{nameof(parameters.Availability)}::media.external_availability IS NULL OR channels.availability = @{nameof(parameters.Availability)})
             ORDER BY {parameters.SortBy.SortExpression} {parameters.SortDirection.Name} NULLS LAST, channels.id
             LIMIT @{nameof(parameters.Limit)}
             OFFSET @{nameof(parameters.Offset)})

         SELECT channels.id,
                channels.created_at,
                channels.created_by_user_id,
                channels.modified_at,
                channels.modified_by_user_id,
                channels.owner_id,
                channels.name,
                channels.storage_path,
                channels.external_id,
                channels.subscribed_at,
                channels.subscriber_count,
                channels.external_url,
                channels.availability,
                channels.primary_library_id,
                channels.video_count,
                channels.total_count,
                
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
         FROM filtered channels
             LEFT JOIN media.channel_images ON channels.id = channel_images.channel_id
             LEFT JOIN media.image_files ON channel_images.image_id = image_files.id
         ORDER BY {parameters.SortBy.SortExpression} {parameters.SortDirection.Name} NULLS LAST, channels.id;
         """;

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
