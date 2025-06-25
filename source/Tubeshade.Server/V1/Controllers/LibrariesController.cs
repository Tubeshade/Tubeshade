using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.V1.Models;

namespace Tubeshade.Server.V1.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class LibrariesController : ControllerBase
{
    private readonly LibraryRepository _repository;

    public LibrariesController(LibraryRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async IAsyncEnumerable<Library> Get([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        await foreach (var entity in _repository.GetUnbufferedAsync(userId).WithCancellation(cancellationToken))
        {
            yield return new Library
            {
                Id = entity.Id,
                CreatedAt = entity.CreatedAt,
                ModifiedAt = entity.ModifiedAt,
                Name = entity.Name,
            };
        }
    }
}
