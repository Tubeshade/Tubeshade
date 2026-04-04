using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media;

public sealed class LibraryCookieRepository
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
             WITH accessible AS
                 (SELECT libraries.id
                  FROM media.libraries
                      INNER JOIN identity.owners ON owners.id = libraries.owner_id
                      INNER JOIN identity.ownerships ON
                          ownerships.owner_id = owners.id AND
                          ownerships.user_id = @{nameof(userId)} AND
                          (ownerships.access = @{nameof(access)} OR ownerships.access = 'owner'))

             SELECT cookies.id AS Id,
                    cookies.created_at AS CreatedAt,
                    cookies.created_by_user_id AS CreatedByUserId,
                    cookies.modified_at AS ModifiedAt,
                    cookies.modified_by_user_id AS ModifiedByUserId,
                    cookies.domain AS Domain,
                    cookies.cookie AS Cookie
             FROM media.library_external_cookies cookies
                 INNER JOIN media.libraries ON cookies.id = libraries.id
             WHERE libraries.id = @{nameof(libraryId)}
               AND libraries.id IN (SELECT id FROM accessible)
               AND domain = @{nameof(domain)};
             """,
            new { userId, libraryId, access, domain },
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
             WITH accessible AS
                 (SELECT libraries.id
                  FROM media.libraries
                      INNER JOIN identity.owners ON owners.id = libraries.owner_id
                      INNER JOIN identity.ownerships ON
                          ownerships.owner_id = owners.id AND
                          ownerships.user_id = @{nameof(entity.ModifiedByUserId)} AND
                          (ownerships.access = 'append' OR ownerships.access = 'owner')),
                 new_cookie AS
                 (SELECT @{nameof(entity.Id)} AS id,
                         @{nameof(entity.CreatedByUserId)} AS created_by_user_id,
                         @{nameof(entity.ModifiedByUserId)} AS modified_by_user_id,
                         @{nameof(entity.Domain)} AS domain,
                         @{nameof(entity.Cookie)} AS cookie
                  WHERE EXISTS (SELECT 1 FROM accessible WHERE accessible.id = @{nameof(entity.Id)}))

             INSERT INTO media.library_external_cookies (id, created_by_user_id, modified_by_user_id, domain, cookie)
             SELECT id, created_by_user_id, modified_by_user_id, domain, cookie FROM new_cookie
             RETURNING id;
             """,
            entity,
            transaction,
            cancellationToken: cancellationToken);

        return await _connection.QuerySingleOrDefaultAsync<Guid>(command);
    }

    public async ValueTask<int> UpdateAsync(
        LibraryCookieEntity entity,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(
            // lang=sql
            $"""
             WITH accessible AS
                 (SELECT libraries.id
                  FROM media.libraries
                      INNER JOIN identity.owners ON owners.id = libraries.owner_id
                      INNER JOIN identity.ownerships ON
                          ownerships.owner_id = owners.id AND
                          ownerships.user_id = @{nameof(entity.ModifiedByUserId)} AND
                          (ownerships.access = 'modify' OR ownerships.access = 'owner'))

             UPDATE media.library_external_cookies
             SET modified_at = CURRENT_TIMESTAMP,
                 modified_by_user_id = @{nameof(entity.ModifiedByUserId)},
                 domain = @{nameof(entity.Domain)},
                 cookie = @{nameof(entity.Cookie)}
             WHERE id = @Id
               AND EXISTS(SELECT 1 FROM accessible WHERE accessible.id = @Id);
             """,
            entity,
            transaction,
            cancellationToken: cancellationToken);

        return await _connection.ExecuteAsync(command);
    }
}
