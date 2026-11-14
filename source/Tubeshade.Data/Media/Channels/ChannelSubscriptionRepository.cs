using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Channels;

/// <remarks>A subscription shares the ids of its channel, and access is checked on the channel's owner.</remarks>
public sealed partial class ChannelSubscriptionRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<ChannelSubscriptionEntity>(connection)
{
    /// <remarks>Added by users who can update the channel.</remarks>
    protected override string InsertSql => Queries.Insert;

    protected override Permissions InsertPermissions => Permissions.Update;

    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;

    public async ValueTask<int> UpdateForSystemAsync(
        SystemPrincipal system,
        ChannelSubscriptionEntity entity,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(Queries.UpdateForSystem, entity, transaction);
        return await Connection.ExecuteAsync(command);
    }

    public async ValueTask<int> DeleteForSystemAsync(
        SystemPrincipal system,
        long id,
        NpgsqlTransaction transaction)
    {
        var command = new CommandDefinition(Queries.DeleteForSystem, new { id }, transaction);
        return await Connection.ExecuteAsync(command);
    }

    public IAsyncEnumerable<ChannelSubscriptionEntity> GetExpiringForSystemAsync(SystemPrincipal system)
    {
        return Connection.QueryUnbufferedAsync<ChannelSubscriptionEntity>(Queries.GetExpiringForSystem);
    }
}
