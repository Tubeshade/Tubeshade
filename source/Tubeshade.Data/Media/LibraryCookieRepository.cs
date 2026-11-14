using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media;

public sealed partial class LibraryCookieRepository
{
    private readonly NpgsqlConnection _connection;

    public LibraryCookieRepository(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public ValueTask<LibraryCookieEntity?> FindByDomain(
        string domain,
        Guid userId,
        Guid libraryId,
        Permissions permissions,
        CancellationToken cancellationToken = default)
    {
        return FindByDomainCore(domain, userId, libraryId, permissions, null, cancellationToken);
    }

    public ValueTask<LibraryCookieEntity?> FindByDomain(
        string domain,
        Guid userId,
        Guid libraryId,
        Permissions permissions,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        return FindByDomainCore(domain, userId, libraryId, permissions, transaction, cancellationToken);
    }

    private async ValueTask<LibraryCookieEntity?> FindByDomainCore(
        string domain,
        Guid userId,
        Guid libraryId,
        Permissions permissions,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.FindByDomain,
            Access.Parameters(new UserPrincipal(userId), Resources.LibraryCookie, permissions | Permissions.Read, new { libraryId, domain }),
            transaction,
            cancellationToken: cancellationToken);

        return await _connection.QuerySingleOrDefaultAsync<LibraryCookieEntity>(command);
    }

    public async ValueTask<Guid?> AddAsync(
        LibraryCookieEntity entity,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.Insert,
            Access.Parameters(
                new UserPrincipal(entity.ModifiedByUserId),
                Resources.LibraryCookie,
                Permissions.Create,
                new { entity.Id, entity.CreatedByUserId, entity.ModifiedByUserId, entity.Domain, entity.Cookie }),
            transaction,
            cancellationToken: cancellationToken);

        return await _connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }

    public async ValueTask<int> UpdateAsync(
        LibraryCookieEntity entity,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            Queries.Update,
            Access.Parameters(
                new UserPrincipal(entity.ModifiedByUserId),
                Resources.LibraryCookie,
                Permissions.Update,
                new { entity.Id, entity.ModifiedByUserId, entity.Domain, entity.Cookie }),
            transaction,
            cancellationToken: cancellationToken);

        return await _connection.ExecuteAsync(command);
    }
}
