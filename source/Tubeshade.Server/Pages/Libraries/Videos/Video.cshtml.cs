using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Htmx;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Videos;
using Tubeshade.Server.Services;
using static System.IO.UnixFileMode;

namespace Tubeshade.Server.Pages.Libraries.Videos;

public sealed class Video : LibraryPageBase
{
    private readonly VideoRepository _videoRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly LibraryRepository _libraryRepository;
    private readonly PreferencesRepository _preferencesRepository;
    private readonly SponsorBlockSegmentRepository _sponsorBlockSegmentRepository;
    private readonly NpgsqlConnection _connection;
    private readonly VideoFileRepository _fileRepository;
    private readonly IClock _clock;
    private readonly ILogger<Video> _logger;
    private readonly TaskService _taskService;

    public Video(
        VideoRepository videoRepository,
        ChannelRepository channelRepository,
        LibraryRepository libraryRepository,
        PreferencesRepository preferencesRepository,
        SponsorBlockSegmentRepository sponsorBlockSegmentRepository,
        NpgsqlConnection connection,
        VideoFileRepository fileRepository,
        IClock clock,
        ILogger<Video> logger,
        TaskService taskService)
    {
        _videoRepository = videoRepository;
        _channelRepository = channelRepository;
        _libraryRepository = libraryRepository;
        _preferencesRepository = preferencesRepository;
        _sponsorBlockSegmentRepository = sponsorBlockSegmentRepository;
        _connection = connection;
        _fileRepository = fileRepository;
        _clock = clock;
        _logger = logger;
        _taskService = taskService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid VideoId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? FileId { get; set; }

    public VideoEntity Entity { get; set; } = null!;

    public List<VideoFileEntity> Files { get; set; } = [];

    public List<VideoFileEntity> PlayableFiles { get; set; } = [];

    public List<VideoFileEntity> DownloadableFiles { get; set; } = [];

    public ChannelEntity Channel { get; set; } = null!;

    public LibraryEntity Library { get; set; } = null!;

    public decimal PlaybackSpeed { get; set; } = 1.0m;

    public bool HasSubtitles { get; set; }

    public bool HasChapters { get; set; }

    public bool HasSponsorBlockSegments { get; set; }

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Entity = await _videoRepository.GetAsync(VideoId, userId, cancellationToken);
        Files = await _videoRepository.GetFilesAsync(VideoId, userId, cancellationToken);
        PlayableFiles = Files.Where(file => file.DownloadedAt is not null || file.TempPath is not null).ToList();
        DownloadableFiles = Files.Where(file => file.DownloadedAt is null && file.TempPath is null).ToList();

        Channel = await _channelRepository.GetAsync(Entity.ChannelId, userId, cancellationToken);
        Library = await _libraryRepository.GetAsync(LibraryId, userId, cancellationToken);

        var preferences = await _preferencesRepository.GetEffectiveForVideo(LibraryId, VideoId, userId, cancellationToken);
        if (preferences?.PlaybackSpeed is { } playbackSpeed)
        {
            PlaybackSpeed = playbackSpeed;
        }

        HasSubtitles = System.IO.File.Exists(Entity.GetSubtitlesFilePath());
        HasChapters = System.IO.File.Exists(Entity.GetChaptersFilePath());

        var segments = await _sponsorBlockSegmentRepository.GetForVideo(VideoId, userId, cancellationToken);
        HasSponsorBlockSegments = segments is not [];
    }

    public async Task<IActionResult> OnPostViewed(string? viewed, Guid videoId)
    {
        var userId = User.GetUserId();

        await _connection.ExecuteWithinTransaction(
            _logger,
            async transaction =>
            {
                if (viewed is not null)
                {
                    await _videoRepository.MarkAsWatched(videoId, userId, transaction);
                }
                else
                {
                    await _videoRepository.MarkAsNotWatched(videoId, userId, transaction);
                }
            });

        return StatusCode(StatusCodes.Status200OK);
    }

    public async Task<IActionResult> OnPostScan()
    {
        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var video = await _videoRepository.FindAsync(VideoId, userId, Access.Modify, transaction);
        if (video is null)
        {
            return NotFound();
        }

        await _taskService.IndexVideo(userId, LibraryId, video, transaction);
        await transaction.CommitAsync();

        return StatusCode(StatusCodes.Status204NoContent);
    }

    public async Task<IActionResult> OnPostDownload()
    {
        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var video = await _videoRepository.FindAsync(VideoId, userId, Access.Modify, transaction);
        if (video is null)
        {
            return NotFound();
        }

        await _taskService.DownloadVideo(userId, LibraryId, VideoId, transaction);
        await transaction.CommitAsync();

        return StatusCode(StatusCodes.Status204NoContent);
    }

    public async Task<IActionResult> OnPostDelete()
    {
        var userId = User.GetUserId();
        await using var transaction = await _connection.OpenAndBeginTransaction();
        var video = await _videoRepository.FindAsync(VideoId, userId, Access.Delete, transaction);
        if (video is null)
        {
            return NotFound();
        }

        var files = await _videoRepository.GetFilesAsync(VideoId, userId, transaction);
        foreach (var file in files)
        {
            var count = await _fileRepository.DeleteAsync(file.Id, userId, transaction);
            Trace.Assert(count is 1);
        }

        if (OperatingSystem.IsLinux())
        {
            var permissions = System.IO.File.GetUnixFileMode(video.StoragePath);
            if (!(permissions.HasFlag(OtherExecute) || permissions.HasFlag(GroupExecute) || permissions.HasFlag(UserExecute)) ||
                !(permissions.HasFlag(OtherWrite) || permissions.HasFlag(GroupWrite) || permissions.HasFlag(UserWrite)))
            {
                throw new UnauthorizedAccessException(
                    $"Missing required privileges in order to delete {video.StoragePath}");
            }
        }

        video.IgnoredAt = _clock.GetCurrentInstant();
        video.IgnoredByUserId = userId;

        var updateCount = await _videoRepository.UpdateAsync(video, transaction);
        Trace.Assert(updateCount is not 0);

        await transaction.CommitAsync();
        _logger.LogInformation("Deleting video directory {Path}", video.StoragePath);
        Directory.Delete(video.StoragePath, true);
        Directory.CreateDirectory(video.StoragePath);

        if (!Request.IsHtmx())
        {
            return RedirectToPage("/Libraries/Channels/Channel", new { LibraryId, video.ChannelId });
        }

        Response.Htmx(headers =>
            headers.Redirect(Url.Page("/Libraries/Channels/Channel", new { LibraryId, video.ChannelId }) ?? ""));
        return StatusCode(StatusCodes.Status200OK);
    }
}
