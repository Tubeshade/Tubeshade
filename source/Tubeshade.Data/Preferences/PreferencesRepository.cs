using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Preferences;

public sealed partial class PreferencesRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<PreferencesEntity>(connection)
{
    protected override string InsertSql => Queries.Insert;

    /// <remarks>Preferences have no owner of their own - access is inherited from the libraries and channels that use them.</remarks>
    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;

    public async ValueTask<PreferencesEntity?> FindForLibrary(
        Guid libraryId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            Queries.FindForLibrary,
            Access.Parameters(new UserPrincipal(userId), Resources.Preferences, Permissions.Read, new { libraryId }),
            transaction));
    }

    public async ValueTask<PreferencesEntity?> FindForLibrary(
        Guid libraryId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            Queries.FindForLibrary,
            Access.Parameters(new UserPrincipal(userId), Resources.Preferences, Permissions.Read, new { libraryId }),
            cancellationToken: cancellationToken));
    }


    public async ValueTask<int> LinkToLibrary(
        Guid preferenceId,
        Guid libraryId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            new CommandDefinition(
                Queries.LinkToLibrary,
                Access.Parameters(new UserPrincipal(userId), Resources.Preferences, Permissions.Update, new { preferenceId, libraryId }),
                transaction));
    }

    public async ValueTask<PreferencesEntity?> GetEffectiveForLibrary(
        Guid libraryId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            Queries.GetEffectiveForLibrary,
            Access.Parameters(new UserPrincipal(userId), Resources.Preferences, Permissions.Read, new { libraryId }),
            cancellationToken: cancellationToken));
    }

    public async ValueTask<PreferencesEntity?> FindForChannel(
        Guid channelId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            Queries.FindForChannel,
            Access.Parameters(new UserPrincipal(userId), Resources.Preferences, Permissions.Read, new { channelId }),
            transaction));
    }

    public async ValueTask<PreferencesEntity?> FindForChannel(
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            Queries.FindForChannel,
            Access.Parameters(new UserPrincipal(userId), Resources.Preferences, Permissions.Read, new { channelId }),
            cancellationToken: cancellationToken));
    }


    public async ValueTask<int> LinkToChannel(
        Guid preferenceId,
        Guid channelId,
        Guid userId,
        NpgsqlTransaction transaction)
    {
        return await Connection.ExecuteAsync(
            new CommandDefinition(
                Queries.LinkToChannel,
                Access.Parameters(new UserPrincipal(userId), Resources.Preferences, Permissions.Update, new { preferenceId, channelId }),
                transaction));
    }

    public ValueTask<PreferencesEntity?> GetEffectiveForChannel(
        Guid libraryId,
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return GetEffectiveForChannelCore(libraryId, channelId, userId, null, cancellationToken);
    }

    public ValueTask<PreferencesEntity?> GetEffectiveForChannel(
        Guid libraryId,
        Guid channelId,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        return GetEffectiveForChannelCore(libraryId, channelId, userId, transaction, cancellationToken);
    }

    private async ValueTask<PreferencesEntity?> GetEffectiveForChannelCore(
        Guid libraryId,
        Guid channelId,
        Guid userId,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            Queries.GetEffectiveForChannel,
            Access.Parameters(new UserPrincipal(userId), Resources.Preferences, Permissions.Read, new { libraryId, channelId }),
            transaction,
            cancellationToken: cancellationToken));
    }

    public async ValueTask<PreferencesEntity?> GetEffectiveForVideo(
        Guid libraryId,
        Guid videoId,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        return await Connection.QuerySingleOrDefaultAsync<PreferencesEntity>(new CommandDefinition(
            Queries.GetEffectiveForVideo,
            Access.Parameters(new UserPrincipal(userId), Resources.Preferences, Permissions.Read, new { libraryId, videoId }),
            transaction,
            cancellationToken: cancellationToken));
    }
}
