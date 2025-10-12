using System;
using System.Collections.Generic;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed class ChannelSubscriptionRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<ChannelSubscriptionEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.channel_subscriptions";

    /// <inheritdoc />
    protected override string InsertSql =>
        """
        INSERT INTO media.channel_subscriptions (id, created_by_user_id, modified_by_user_id, status, callback, topic, expires_at, verify_token, secret) 
        VALUES (@Id, @CreatedByUserId, @ModifiedByUserId, @Status, @Callback, @Topic, @ExpiresAt, @VerifyToken, @Secret)
        RETURNING id;
        """;

    /// <inheritdoc />
    protected override string AccessCte =>
        $"""
         WITH accessible AS
         (SELECT channels.id
          FROM media.channels
                   INNER JOIN identity.owners ON owners.id = channels.owner_id
                   INNER JOIN identity.ownerships ON
              ownerships.owner_id = owners.id AND
              ownerships.user_id = @{nameof(GetParameters.UserId)} AND
              (ownerships.access = @{nameof(GetParameters.Access)} OR ownerships.access = 'owner'))
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        """
        SELECT id AS Id,
               created_at AS CreatedAt,
               created_by_user_id AS CreatedByUserId,
               modified_at AS ModifiedAt,
               modified_by_user_id AS ModifiedByUserId,
               status AS Status,
               callback AS Callback,
               topic AS Topic,
               expires_at AS ExpiresAt,
               verify_token AS VerifyToken,
               secret AS Secret
        FROM media.channel_subscriptions
        """;

    /// <inheritdoc />
    protected override string UpdateSet => throw new NotSupportedException();

    /// <inheritdoc />
    protected override string UpdateSql =>
        $"""
         UPDATE media.channel_subscriptions
         SET modified_at = CURRENT_TIMESTAMP,
             modified_by_user_id = @{nameof(ChannelSubscriptionEntity.ModifiedByUserId)},
             status = @{nameof(ChannelSubscriptionEntity.Status)},
             callback = @{nameof(ChannelSubscriptionEntity.Callback)},
             topic = @{nameof(ChannelSubscriptionEntity.Topic)},
             expires_at = @{nameof(ChannelSubscriptionEntity.ExpiresAt)},
             verify_token = @{nameof(ChannelSubscriptionEntity.VerifyToken)},
             secret = @{nameof(ChannelSubscriptionEntity.Secret)}
         WHERE (media.channel_subscriptions.id = @{nameof(ChannelSubscriptionEntity.Id)});
         """;

    public IAsyncEnumerable<ChannelSubscriptionEntity> GetExpiringUnbufferedAsync()
    {
        return Connection.QueryUnbufferedAsync<ChannelSubscriptionEntity>(
            $"""
             {SelectSql}
             WHERE expires_at IS NOT NULL AND (CURRENT_TIMESTAMP - expires_at) >= '-2 days'::interval;
             """);
    }
}
