using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
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
    private readonly LibraryCookieRepository _repository;

    public CookiesController(NpgsqlConnection connection, LibraryCookieRepository repository)
    {
        _connection = connection;
        _repository = repository;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PostAsync(CookieUpdateRequest request)
    {
        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var cookie = await _repository.FindByDomain(request.Domain, userId, request.LibraryId!.Value, Access.Read, transaction);

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

            if (await _repository.AddAsync(cookie, transaction) is null)
            {
                return Problem("Missing permissions to modify cookies", statusCode: StatusCodes.Status403Forbidden);
            }
        }
        else
        {
            cookie.ModifiedByUserId = userId;
            cookie.Cookie = request.Cookie;

            if (await _repository.UpdateAsync(cookie, transaction) is 0)
            {
                return Problem("Missing permissions to modify cookies", statusCode: StatusCodes.Status403Forbidden);
            }
        }

        await transaction.CommitAsync();

        return NoContent();
    }
}
