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

public sealed class ChannelRepository(NpgsqlConnection connection) : ModifiableRepositoryBase<ChannelEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.channels";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO media.channels (created_by_user_id, modified_by_user_id, owner_id, name, storage_path, external_id, subscribed_at, subscriber_count, external_url, availability) 
         VALUES (@CreatedByUserId, @ModifiedByUserId, @OwnerId, @Name, @StoragePath, @ExternalId, @SubscribedAt, @SubscriberCount, @ExternalUrl, @Availability)
         RETURNING id;
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        $"""
         SELECT id AS Id,
                created_at AS CreatedAt,
                created_by_user_id AS CreatedByUserId,
                modified_at AS ModifiedAt,
                modified_by_user_id AS ModifiedByUserId,
                owner_id AS OwnerId,
                name AS Name,
                storage_path AS StoragePath,
                external_id AS ExternalId,
                subscribed_at AS SubscribedAt,
                subscriber_count AS SubscriberCount,
                external_url AS ExternalUrl,
                availability AS Availability
         FROM media.channels
         """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        $"""
         name = @Name,
         storage_path = @StoragePath,
         external_id = @ExternalId,
         subscribed_at = @SubscribedAt
         """;

    public async ValueTask<List<ChannelEntity>> GetForLibrary(Guid libraryId, Guid userId, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             {AccessCte}
             
             SELECT id AS Id,
                    created_at AS CreatedAt,
                    created_by_user_id AS CreatedByUserId,
                    modified_at AS ModifiedAt,
                    modified_by_user_id AS ModifiedByUserId,
                    owner_id AS OwnerId,
                    name AS Name,
                    storage_path AS StoragePath,
                    external_id AS ExternalId,
                    subscribed_at AS SubscribedAt,
                    subscriber_count AS SubscriberCount,
                    external_url AS ExternalUrl,
                    availability AS Availability
             FROM media.channels
             WHERE {AccessFilter}
                AND EXISTS(SELECT 1
                           FROM media.libraries
                               INNER JOIN media.library_channels ON libraries.id = library_channels.library_id
                           WHERE libraries.id = @{nameof(GetFromLibraryParameters.LibraryId)}
                             AND library_channels.channel_id = channels.id);
             """,
            new GetFromLibraryParameters(userId, libraryId, Access.Read),
            cancellationToken: cancellationToken);

        var enumerable = await Connection.QueryAsync<ChannelEntity>(command);
        return enumerable as List<ChannelEntity> ?? enumerable.ToList();
    }
}
