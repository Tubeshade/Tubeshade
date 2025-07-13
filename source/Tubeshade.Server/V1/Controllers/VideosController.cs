using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.V1.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]/{id:guid}")]
public sealed class VideosController : ControllerBase
{
    private readonly VideoRepository _repository;

    public VideosController(VideoRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var video = await _repository.GetAsync(id, User.GetUserId(), cancellationToken);

        var stream = System.IO.File.OpenRead(video.StoragePath);
        return File(
            stream,
            $"video/{Path.GetExtension(video.StoragePath)}",
            video.ModifiedAt.ToDateTimeOffset(),
            new EntityTagHeaderValue(new StringSegment($"\"{id}\"")),
            true);
    }

    [HttpGet("Files")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Client)]
    public async Task<List<VideoFileEntity>> GetFiles(Guid id, CancellationToken cancellationToken)
    {
        return await _repository.GetFilesAsync(id, User.GetUserId(), cancellationToken);
    }

    [HttpGet("Files/{fileId:guid}")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetFile(Guid id, Guid fileId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var file = await _repository.FindFileAsync(fileId, userId, cancellationToken);
        if (file?.DownloadedAt is null)
        {
            return NotFound();
        }

        var video = await _repository.GetAsync(file.VideoId, userId, cancellationToken);
        var stream = System.IO.File.OpenRead(Path.Combine(video.StoragePath, file.StoragePath));
        return File(
            stream,
            $"video/{file.Type.Name}",
            file.CreatedAt.ToDateTimeOffset(),
            new EntityTagHeaderValue(new StringSegment($"\"{id}\"")),
            true);
    }

    [HttpGet("Thumbnail")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetThumbnail(Guid id, CancellationToken cancellationToken)
    {
        var video = await _repository.GetAsync(id, User.GetUserId(), cancellationToken);
        var attributes = System.IO.File.GetAttributes(video.StoragePath);
        var path = (attributes & FileAttributes.Directory) is FileAttributes.Directory
            ? Path.Combine(video.StoragePath, "thumbnail.jpg")
            : video.StoragePath.Replace(Path.GetFileName(video.StoragePath), "thumbnail.jpg");

        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var file = new FileInfo(path);
        var stream = file.OpenRead();
        return File(
            stream,
            "image/jpeg",
            "thumbnail.jpg",
            file.LastWriteTimeUtc,
            new EntityTagHeaderValue(new StringSegment($"\"thumbnail_{id}_{file.LastWriteTimeUtc}\"")));
    }

    [HttpGet("Subtitles")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetSubtitles(Guid id, CancellationToken cancellationToken)
    {
        var video = await _repository.GetAsync(id, User.GetUserId(), cancellationToken);
        var attributes = System.IO.File.GetAttributes(video.StoragePath);
        var path = (attributes & FileAttributes.Directory) is FileAttributes.Directory
            ? Path.Combine(video.StoragePath, "subtitles.en.vtt")
            : video.StoragePath.Replace(Path.GetFileName(video.StoragePath), "subtitles.en.vtt");

        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var file = new FileInfo(path);
        var stream = file.OpenRead();
        return File(
            stream,
            "text/vtt",
            file.LastWriteTimeUtc,
            new EntityTagHeaderValue(new StringSegment($"\"subtitles_en_{id}_{file.LastWriteTimeUtc}\"")));
    }

    [HttpGet("Chapters")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetChapters(Guid id, CancellationToken cancellationToken)
    {
        var video = await _repository.GetAsync(id, User.GetUserId(), cancellationToken);
        var attributes = System.IO.File.GetAttributes(video.StoragePath);
        var path = (attributes & FileAttributes.Directory) is FileAttributes.Directory
            ? Path.Combine(video.StoragePath, "chapters.vtt")
            : video.StoragePath.Replace(Path.GetFileName(video.StoragePath), "chapters.vtt");

        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var file = new FileInfo(path);
        var stream = file.OpenRead();
        return File(
            stream,
            "text/vtt",
            file.LastWriteTimeUtc,
            new EntityTagHeaderValue(new StringSegment($"\"chapters_{id}_{file.LastWriteTimeUtc}\"")));
    }
}
