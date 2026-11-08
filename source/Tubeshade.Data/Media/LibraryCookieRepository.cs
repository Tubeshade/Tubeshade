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
    private const string AccessibleLibrariesCte = RepositoryBase<LibraryCookieEntity>.AccessibleLibrariesCte;

    private readonly NpgsqlConnection _connection;

    public LibraryCookieRepository(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public ValueTask<LibraryCookieEntity?> FindByDomain(
        string domain,
        Guid userId,
        Guid libraryId,
        Access access,
        CancellationToken cancellationToken = default)
    {
        return FindByDomainCore(domain, userId, libraryId, access, null, cancellationToken);
    }

    public ValueTask<LibraryCookieEntity?> FindByDomain(
        string domain,
        Guid userId,
        Guid libraryId,
        Access access,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        return FindByDomainCore(domain, userId, libraryId, access, transaction, cancellationToken);
    }

    private async ValueTask<LibraryCookieEntity?> FindByDomainCore(
        string domain,
        Guid userId,
        Guid libraryId,
        Access access,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             WITH {AccessibleLibrariesCte}

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
               AND libraries.id IN (SELECT id FROM accessible_libraries)
               AND domain = @{nameof(domain)};
             """,
            new { UserId = userId, libraryId, Access = access, domain },
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
             WITH {AccessibleLibrariesCte},
                 new_cookie AS
                 (SELECT @{nameof(entity.Id)} AS id,
                         @{nameof(entity.CreatedByUserId)} AS created_by_user_id,
                         @{nameof(entity.ModifiedByUserId)} AS modified_by_user_id,
                         @{nameof(entity.Domain)} AS domain,
                         @{nameof(entity.Cookie)} AS cookie
                  WHERE EXISTS (SELECT 1 FROM accessible_libraries WHERE accessible_libraries.id = @{nameof(entity.Id)}))

             INSERT INTO media.library_external_cookies (id, created_by_user_id, modified_by_user_id, domain, cookie)
             SELECT id, created_by_user_id, modified_by_user_id, domain, cookie FROM new_cookie
             RETURNING id;
             """,
            new
            {
                entity.Id,
                entity.CreatedByUserId,
                entity.ModifiedByUserId,
                entity.Domain,
                entity.Cookie,
                UserId = entity.ModifiedByUserId,
                Access = Access.Append,
            },
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
             WITH {AccessibleLibrariesCte}

             UPDATE media.library_external_cookies
             SET modified_at = CURRENT_TIMESTAMP,
                 modified_by_user_id = @{nameof(entity.ModifiedByUserId)},
                 domain = @{nameof(entity.Domain)},
                 cookie = @{nameof(entity.Cookie)}
             WHERE id = @Id
               AND EXISTS(SELECT 1 FROM accessible_libraries WHERE accessible_libraries.id = @Id);
             """,
            new
            {
                entity.Id,
                entity.ModifiedByUserId,
                entity.Domain,
                entity.Cookie,
                UserId = entity.ModifiedByUserId,
                Access = Access.Modify,
            },
            transaction,
            cancellationToken: cancellationToken);

        return await _connection.ExecuteAsync(command);
    }
}
