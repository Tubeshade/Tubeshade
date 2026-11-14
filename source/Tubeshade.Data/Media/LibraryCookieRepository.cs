using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media;

public sealed class LibraryCookieRepository
{
    private const string AccessibleLibraries = RepositoryBase<LibraryCookieEntity>.AccessibleLibraries;

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
            // lang=sql
            $"""
             SELECT cookies.id,
                    cookies.created_at,
                    cookies.created_by_user_id,
                    cookies.modified_at,
                    cookies.modified_by_user_id,
                    cookies.domain,
                    cookies.cookie
             FROM media.library_external_cookies cookies
                 INNER JOIN media.libraries ON cookies.id = libraries.id
             WHERE libraries.id = @{nameof(libraryId)}
               AND libraries.id IN ({AccessibleLibraries})
               AND domain = @{nameof(domain)};
             """,
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
            // lang=sql
            $"""
             WITH new_cookie AS
                 (SELECT @{nameof(entity.Id)} AS id,
                         @{nameof(entity.CreatedByUserId)} AS created_by_user_id,
                         @{nameof(entity.ModifiedByUserId)} AS modified_by_user_id,
                         @{nameof(entity.Domain)} AS domain,
                         @{nameof(entity.Cookie)} AS cookie
                  WHERE @{nameof(entity.Id)} IN ({AccessibleLibraries}))

             INSERT INTO media.library_external_cookies (id, created_by_user_id, modified_by_user_id, domain, cookie)
             SELECT id, created_by_user_id, modified_by_user_id, domain, cookie FROM new_cookie
             RETURNING id;
             """,
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
            // lang=sql
            $"""
             UPDATE media.library_external_cookies
             SET modified_at = CURRENT_TIMESTAMP,
                 modified_by_user_id = @{nameof(entity.ModifiedByUserId)},
                 domain = @{nameof(entity.Domain)},
                 cookie = @{nameof(entity.Cookie)}
             WHERE id = @Id
               AND id IN ({AccessibleLibraries});
             """,
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
