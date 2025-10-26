using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Htmx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using NodaTime.Text;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Videos;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.V1.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]/{id:guid}")]
public sealed class VideosController : ControllerBase
{
    private readonly VideoRepository _repository;
    private readonly SponsorBlockSegmentRepository _segmentRepository;
    private readonly WebVideoTextTracksService _webVideoTextTracksService;
    private readonly FileUploadService _fileUploadService;

    public VideosController(
        VideoRepository repository,
        SponsorBlockSegmentRepository segmentRepository,
        WebVideoTextTracksService webVideoTextTracksService,
        FileUploadService fileUploadService)
    {
        _repository = repository;
        _segmentRepository = segmentRepository;
        _webVideoTextTracksService = webVideoTextTracksService;
        _fileUploadService = fileUploadService;
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

    [HttpPost("Files")]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [MultipartFormData]
    [DisableFormValueModelBinding]
    [DisableRequestSizeLimit]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadFile(Guid id, [FromQuery] Guid libraryId, CancellationToken cancellationToken)
    {
        var fileId = await _fileUploadService.UploadVideoFile(
            User.GetUserId(),
            id,
            Request.Body,
            Request.ContentType,
            cancellationToken);

        // The preferred solution would be to handle this in the page itself.
        // At the time of implementing this I could not find a way to not buffer the entire file in memory
        // in the page itself, so htmx is also referenced in the controller.
        if (Request.IsHtmx())
        {
            var redirect = Url.Page("/Libraries/Videos/Video", new { libraryId, videoId = id, fileId });
            Response.Htmx(headers => headers.Redirect(redirect ?? string.Empty));
        }

        return CreatedAtAction(nameof(GetFile), new { version = "1.0", id, fileId }, fileId);
    }

    [HttpGet("Files/{fileId:guid}")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetFile(Guid id, Guid fileId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var videoFile = await _repository.FindFileAsync(fileId, userId, cancellationToken);
        if (videoFile?.DownloadedAt is null)
        {
            return NotFound();
        }

        var video = await _repository.GetAsync(videoFile.VideoId, userId, cancellationToken);
        var file = new FileInfo(Path.Combine(video.StoragePath, videoFile.StoragePath));
        var stream = file.OpenRead();
        return File(
            stream,
            $"video/{videoFile.Type.Name}",
            videoFile.CreatedAt.ToDateTimeOffset(),
            new EntityTagHeaderValue(new StringSegment($"\"video_{id}_{file.LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)}\"")),
            true);
    }

    [HttpGet("Thumbnail")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetThumbnail(Guid id, CancellationToken cancellationToken)
    {
        var video = await _repository.GetAsync(id, User.GetUserId(), cancellationToken);
        var path = video.GetThumbnailFilePath();

        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var file = new FileInfo(path);
        var stream = file.OpenRead();
        return File(
            stream,
            "image/jpeg",
            file.LastWriteTimeUtc,
            new EntityTagHeaderValue(new StringSegment($"\"thumbnail_{id}_{file.LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)}\"")));
    }

    [HttpGet("Subtitles")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetSubtitles(Guid id, CancellationToken cancellationToken)
    {
        var video = await _repository.GetAsync(id, User.GetUserId(), cancellationToken);
        var path = video.GetSubtitlesFilePath();

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
            new EntityTagHeaderValue(new StringSegment($"\"subtitles_en_{id}_{file.LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)}\"")));
    }

    [HttpGet("Chapters")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetChapters(Guid id, CancellationToken cancellationToken)
    {
        var video = await _repository.GetAsync(id, User.GetUserId(), cancellationToken);
        var path = video.GetChaptersFilePath();

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
            new EntityTagHeaderValue(new StringSegment($"\"chapters_{id}_{file.LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)}\"")));
    }

    [HttpGet("SponsorBlock")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetSponsorBlockSegments(Guid id, CancellationToken cancellationToken)
    {
        var segments = await _segmentRepository.GetForVideo(id, User.GetUserId(), cancellationToken);
        if (segments is [])
        {
            return NoContent();
        }

        var memoryStream = new MemoryStream();
        var cues = segments.Select(TextTrackCue.FromSponsorBlockSegment);

        await _webVideoTextTracksService.Write(memoryStream, cues, cancellationToken);
        memoryStream.Position = 0;

        var modifiedAt = segments.Select(segment => segment.CreatedAt).OrderDescending().First();
        return File(
            memoryStream,
            "text/vtt",
            modifiedAt.ToDateTimeOffset(),
            new EntityTagHeaderValue(new StringSegment($"\"sponsorblock_{id}_{InstantPattern.ExtendedIso.Format(modifiedAt)}\"")));
    }
}
