using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Htmx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using NodaTime.Text;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Videos;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.V1.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]/{id:guid}")]
public sealed class VideosController : ControllerBase
{
    private readonly NpgsqlConnection _connection;
    private readonly VideoRepository _repository;
    private readonly SponsorBlockSegmentRepository _segmentRepository;
    private readonly WebVideoTextTracksService _webVideoTextTracksService;
    private readonly FileUploadService _fileUploadService;

    public VideosController(
        NpgsqlConnection connection,
        VideoRepository repository,
        SponsorBlockSegmentRepository segmentRepository,
        WebVideoTextTracksService webVideoTextTracksService,
        FileUploadService fileUploadService)
    {
        _connection = connection;
        _repository = repository;
        _segmentRepository = segmentRepository;
        _webVideoTextTracksService = webVideoTextTracksService;
        _fileUploadService = fileUploadService;
    }

    [HttpPost("PlaybackPosition")]
    [Consumes(MediaTypeNames.Application.FormUrlEncoded)]
    public async Task<NoContentResult> UpdatePlaybackPosition(Guid id, [Required, FromForm] double? position)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(IsolationLevel.ReadCommitted);
        await _repository.UpdatePlaybackPosition(id, User.GetUserId(), position!.Value, transaction);
        await transaction.CommitAsync();

        return NoContent();
    }

    [HttpGet("Files")]
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
    [ResponseCache(CacheProfileName = CacheProfiles.Static)]
    public async Task<IActionResult> GetFile(Guid id, Guid fileId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction(IsolationLevel.ReadCommitted, cancellationToken);
        var video = await _repository.FindAsync(id, userId, Access.Read, transaction);
        if (video is null)
        {
            return NotFound();
        }

        var videoFile = await _repository.FindFileAsync(fileId, userId, transaction);
        if (videoFile?.DownloadedAt is null)
        {
            return NotFound();
        }

        await transaction.CommitAsync(cancellationToken);

        var file = new FileInfo(Path.Combine(video.StoragePath, videoFile.StoragePath));
        var stream = file.OpenRead();
        return File(
            stream,
            $"video/{videoFile.Type.Name}",
            file.LastWriteTimeUtc,
            new EntityTagHeaderValue(new StringSegment($"\"video_{id}_{file.LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)}\"")),
            true);
    }

    [HttpGet("Thumbnail")]
    [ResponseCache(CacheProfileName = CacheProfiles.Static)]
    public async Task<IActionResult> GetThumbnail(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction(IsolationLevel.ReadCommitted, cancellationToken);
        var video = await _repository.FindAsync(id, userId, Access.Read, transaction);
        if (video is null)
        {
            return NotFound();
        }

        var thumbnail = await _repository.FindThumbnailAsync(id, userId, transaction);
        if (thumbnail is null)
        {
            return NotFound();
        }

        await transaction.CommitAsync(cancellationToken);

        var file = new FileInfo(Path.Combine(video.StoragePath, thumbnail.StoragePath));
        var stream = file.OpenRead();
        return File(
            stream,
            $"image/{Path.GetExtension(thumbnail.StoragePath).Trim('.')}",
            file.LastWriteTimeUtc,
            new EntityTagHeaderValue(new StringSegment($"\"thumbnail_{id}_{file.LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)}\"")));
    }

    [HttpGet("Subtitles")]
    [ResponseCache(CacheProfileName = CacheProfiles.Dynamic)]
    public async Task<IActionResult> GetSubtitles(Guid id, CancellationToken cancellationToken)
    {
        var video = await _repository.FindAsync(id, User.GetUserId(), Access.Read, cancellationToken);
        if (video is null)
        {
            return NotFound();
        }

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
    [ResponseCache(CacheProfileName = CacheProfiles.Dynamic)]
    public async Task<IActionResult> GetChapters(Guid id, CancellationToken cancellationToken)
    {
        var video = await _repository.FindAsync(id, User.GetUserId(), Access.Read, cancellationToken);
        if (video is null)
        {
            return NotFound();
        }

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
    [ResponseCache(CacheProfileName = CacheProfiles.Dynamic)]
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
