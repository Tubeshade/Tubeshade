using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Channels;

/// <remarks>A subscription shares the id of its channel, and access is checked on the channel's owner.</remarks>
public sealed class ChannelSubscriptionRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<ChannelSubscriptionEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.channel_subscriptions";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO media.channel_subscriptions (id, created_by_user_id, modified_by_user_id, status, callback, topic, expires_at, verify_token, secret)
         SELECT @Id, @CreatedByUserId, @ModifiedByUserId, @Status, @Callback, @Topic, @ExpiresAt, @VerifyToken, @Secret
         WHERE @Id IN (SELECT channels.id
                       FROM media.channels
                       WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@{nameof(IEntity.CreatedByUserId)}, {(int)Permissions.Update})))
         RETURNING id;
         """;

    protected override string AccessFilter =>
        $"""
         (channel_subscriptions.id IN (SELECT channels.id FROM media.channels WHERE channels.owner_id IN ({Access.AccessibleOwners})))
         """;

    /// <inheritdoc />
    protected override string UpdateAccessFilter =>
        $"""
         (channel_subscriptions.id IN (SELECT channels.id FROM media.channels WHERE channels.owner_id IN ({UpdatableOwners})))
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        """
        SELECT id,
               created_at,
               created_by_user_id,
               modified_at,
               modified_by_user_id,
               status,
               callback,
               topic,
               expires_at,
               verify_token,
               secret
        FROM media.channel_subscriptions
        """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        $"""
         status = @{nameof(ChannelSubscriptionEntity.Status)},
         callback = @{nameof(ChannelSubscriptionEntity.Callback)},
         topic = @{nameof(ChannelSubscriptionEntity.Topic)},
         expires_at = @{nameof(ChannelSubscriptionEntity.ExpiresAt)},
         verify_token = @{nameof(ChannelSubscriptionEntity.VerifyToken)},
         secret = @{nameof(ChannelSubscriptionEntity.Secret)}
         """;

    public async ValueTask<int> UpdateForSystemAsync(
        SystemPrincipal system,
        ChannelSubscriptionEntity entity,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            $"""
             UPDATE media.channel_subscriptions
             SET modified_at = CURRENT_TIMESTAMP,
             {UpdateSet}
             WHERE channel_subscriptions.id = @{nameof(IEntity.Id)};
             """,
            entity,
            transaction);

        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask<int> DeleteForSystemAsync(
        SystemPrincipal system,
        Guid id,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(
            "DELETE FROM media.channel_subscriptions WHERE id = @id;",
            new { id },
            transaction);

        return await Connection.ExecuteAsync(command);
    }

    public IAsyncEnumerable<ChannelSubscriptionEntity> GetExpiringForSystemAsync(SystemPrincipal system)
    {
        return Connection.QueryUnbufferedAsync<ChannelSubscriptionEntity>(
            $"""
             {SelectSql}
             WHERE expires_at IS NOT NULL AND (CURRENT_TIMESTAMP - expires_at) >= '-2 days'::interval;
             """);
    }
}
