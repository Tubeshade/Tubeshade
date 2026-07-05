using System;
using System.Globalization;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.V1.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]/{id:guid}")]
public sealed class ImagesController : ControllerBase
{
    private readonly NpgsqlConnection _connection;
    private readonly ImageFileRepository _repository;

    public ImagesController(NpgsqlConnection connection, ImageFileRepository repository)
    {
        _connection = connection;
        _repository = repository;
    }

    [HttpGet]
    [ResponseCache(CacheProfileName = CacheProfiles.Static)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

        var image = await _repository.FindAsync(id, userId, Access.Read, transaction);
        var folderPath = await _repository.GetPath(id, transaction, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        if (image is null || folderPath is null)
        {
            Response.NoCache();
            return NotFound();
        }

        var path = Path.Combine(folderPath, image.StoragePath);
        if (!System.IO.File.Exists(path))
        {
            Response.NoCache();
            return Problem("File not found", statusCode: StatusCodes.Status404NotFound);
        }

        var file = new FileInfo(path);
        var contentType = file.Extension.ToUpperInvariant() switch
        {
            ".JPG" or ".JPEG" => MediaTypeNames.Image.Jpeg,
            ".WEBP" => MediaTypeNames.Image.Webp,
            _ => throw new InvalidOperationException($"Unexpected image extension '{file.Extension}'"),
        };

        var etag = image is { HashAlgorithm.Name: not HashAlgorithm.Names.Placeholder, Hash: { } hash }
            ? $"\"{Convert.ToBase64String(hash)}\""
            : $"\"image_{id}_{file.LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)}\"";

        var stream = file.OpenRead();
        return File(
            stream,
            contentType,
            file.LastWriteTimeUtc,
            new EntityTagHeaderValue(new StringSegment(etag)));
    }
}
