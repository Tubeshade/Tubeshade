using Npgsql;
using Tubeshade.Data.Abstractions;

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
}
