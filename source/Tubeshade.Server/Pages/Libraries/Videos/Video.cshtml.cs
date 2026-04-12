using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Htmx;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Services;
using static System.IO.UnixFileMode;
using StringExtensions = Tubeshade.Server.Pages.Shared.StringExtensions;

namespace Tubeshade.Server.Pages.Libraries.Videos;

public sealed partial class Video : LibraryPageBase
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
    private readonly TrackFileRepository _trackFileRepository;

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
        TaskService taskService,
        TrackFileRepository trackFileRepository)
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
        _trackFileRepository = trackFileRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid VideoId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? FileId { get; set; }

    public VideoEntity Entity { get; set; } = null!;

    public IHtmlContent DescriptionContent { get; private set; } = null!;

    public List<VideoFileEntity> Files { get; set; } = [];

    public List<VideoFileEntity> PlayableFiles { get; set; } = [];

    public List<VideoFileEntity> DownloadableFiles { get; set; } = [];

    public ChannelEntity Channel { get; set; } = null!;

    public LibraryEntity Library { get; set; } = null!;

    public decimal PlaybackSpeed { get; set; } = 1.0m;

    public TrackFileEntity? Chapters { get; set; }

    public List<TrackFileEntity> Subtitles { get; set; } = [];

    public bool HasSponsorBlockSegments { get; set; }

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await using var transaction = await _connection.OpenAndBeginTransaction(IsolationLevel.RepeatableRead, cancellationToken);

        Entity = await _videoRepository.GetAsync(VideoId, userId, transaction);
        Files = await _videoRepository.GetFilesAsync(VideoId, userId, transaction, cancellationToken);
        PlayableFiles = Files.Where(file => file.DownloadedAt is not null || file.TempPath is not null).ToList();
        DownloadableFiles = Files.Where(file => file.DownloadedAt is null && file.TempPath is null).ToList();

        Channel = await _channelRepository.GetAsync(Entity.ChannelId, userId, transaction);
        Library = await _libraryRepository.GetAsync(LibraryId, userId, transaction);

        var preferences = await _preferencesRepository.GetEffectiveForVideo(LibraryId, VideoId, userId, transaction, cancellationToken);
        if (preferences?.PlaybackSpeed is { } playbackSpeed)
        {
            PlaybackSpeed = playbackSpeed;
        }

        var trackFiles = await _trackFileRepository.GetForVideo(VideoId, userId, Access.Read, transaction, cancellationToken);
        Chapters = trackFiles.SingleOrDefault(file => file.Type == TrackType.Chapters);
        Subtitles = trackFiles.Where(file => file.Type == TrackType.Subtitles).ToList();

        var segments = await _sponsorBlockSegmentRepository.GetForVideo(VideoId, userId, transaction);
        HasSponsorBlockSegments = segments is not [];

        DescriptionContent = await FormatDescription(userId, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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

        await _taskService.IndexVideo(userId, LibraryId, video, TaskSource.User, transaction);
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

        await _taskService.DownloadVideo(userId, LibraryId, VideoId, TaskSource.User, transaction);
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

    private async Task<HtmlContentBuilder> FormatDescription(
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var description = Entity.Description;

        var urlQueue = new Queue<UrlMatch>(YoutubeVideoUrlRegex().Matches(description).Select(match => new UrlMatch(match)));
        var youTubeIds = urlQueue.Select(match => match.YouTubeId).Distinct().ToList();

        var videoIds = youTubeIds is not []
            ? await _videoRepository.FindByExternalIds(youTubeIds, userId, transaction, cancellationToken)
            : [];

        var videoUrlLookup = videoIds.ToDictionary(
            id => id.ExternalId,
            id => Url.Page("/Libraries/Videos/Video", new { LibraryId, videoId = id.Id }));

        var descriptionSpan = description.AsSpan();
        var builder = new HtmlContentBuilder();
        foreach (var paragraphRange in StringExtensions.ParagraphSplit().EnumerateSplits(descriptionSpan))
        {
            var paragraphSpan = descriptionSpan[paragraphRange];
            if (paragraphSpan.Length is 0 || paragraphSpan.IsWhiteSpace())
            {
                continue;
            }

            // lang=html
            builder.AppendHtml("<p class=\"tubeshade-paragraph\">");

            var currentIndex = paragraphRange.Start.Value;
            while (urlQueue.TryPeek(out var match))
            {
                var range = match.Range;

                if (range.Start.Value > paragraphRange.End.Value)
                {
                    break;
                }

                match = urlQueue.Dequeue();
                if (range.Start.Value < paragraphRange.Start.Value)
                {
                    continue;
                }

                if (!videoUrlLookup.TryGetValue(match.YouTubeId, out var videoUrl) ||
                    string.IsNullOrWhiteSpace(videoUrl))
                {
                    continue;
                }

                builder.Append(descriptionSpan[currentIndex..range.Start].ToString());

                builder.AppendHtml("<a href=\"");
                builder.Append(videoUrl);
                builder.AppendHtml("\">");
                builder.Append(match.Match.Value);
                builder.AppendHtml("</a>");

                currentIndex = range.End.Value;
            }

            builder.Append(descriptionSpan[currentIndex..paragraphRange.End.Value].ToString());

            // lang=html
            builder.AppendHtmlLine("</p>");
        }

        return builder;
    }

    [GeneratedRegex(
        @"(?<=^|\s)(?:https?:\/\/)?(?:(?:www\.)?youtube\.com|youtu\.be)\/(?:watch\?v\=|embed\/|v\/)?(?'id'[a-zA-Z0-9_\-]{11})(?:[^\s]*)",
        RegexOptions.Multiline,
        100)]
    public static partial Regex YoutubeVideoUrlRegex();

    private readonly struct UrlMatch
    {
        public UrlMatch(Match match)
        {
            Match = match;
            YouTubeId = match.Groups["id"].Value;
            Range = new(match.Index, match.Index + match.Length);
        }

        public Match Match { get; }

        public string YouTubeId { get; }

        public Range Range { get; }
    }
}
