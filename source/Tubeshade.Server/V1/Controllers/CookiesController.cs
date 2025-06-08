using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.V1.Models;

namespace Tubeshade.Server.V1.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class CookiesController : ControllerBase
{
    private readonly NpgsqlConnection _connection;

    public CookiesController(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PostAsync(CookieUpdateRequest request)
    {
        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var cookie = await _connection.QuerySingleOrDefaultAsync<LibraryCookieEntity>(new CommandDefinition(
            """
            SELECT cookies.id AS Id,
                   cookies.created_at AS CreatedAt,
                   cookies.created_by_user_id AS CreatedByUserId,
                   cookies.modified_at AS ModifiedAt,
                   cookies.modified_by_user_id AS ModifiedByUserId,
                   cookies.domain AS Domain,
                   cookies.cookie AS Cookie
            FROM media.library_external_cookies cookies
            INNER JOIN media.libraries ON cookies.id = libraries.id
            WHERE libraries.owner_id = @userId AND libraries.id = @libraryId;
            """, // todo: auth
            new { userId, libraryId = request.LibraryId },
            transaction));

        if (cookie is null)
        {
            cookie = new LibraryCookieEntity
            {
                Id = request.LibraryId!.Value,
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                Domain = request.Domain,
                Cookie = request.Cookie,
            };

            var count = await _connection.ExecuteAsync(new CommandDefinition(
                $"""
                 INSERT INTO media.library_external_cookies (id, created_by_user_id, modified_by_user_id, domain, cookie)
                 VALUES (@Id, @CreatedByUserId, @ModifiedByUserId, @Domain, @Cookie);
                 """,
                cookie,
                transaction));
            Trace.Assert(count is not 0);
        }
        else
        {
            cookie.ModifiedByUserId = userId;
            cookie.Domain = request.Domain;
            cookie.Cookie = request.Cookie;

            var count = await _connection.ExecuteAsync(new CommandDefinition(
                $"""
                 UPDATE media.library_external_cookies
                 SET modified_at = CURRENT_TIMESTAMP,
                     domain = @Domain,
                     cookie = @Cookie
                 WHERE id = @Id;
                 """,
                cookie,
                transaction));
            Trace.Assert(count is not 0);
        }

        await transaction.CommitAsync();

        return NoContent();
    }
}
