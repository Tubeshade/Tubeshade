using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Npgsql;
using SponsorBlock;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration;
using Tubeshade.Server.Pages.Shared;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;
using static YoutubeDLSharp.Metadata.Availability;

namespace Tubeshade.Server.Services;

public sealed class YoutubeService
{
    private static readonly string[] VideoFormats =
    [
        "bv+(ba[format_note*=original]/ba)/best",
        "bv*[height<=720]+(ba[format_note*=original]/ba)"
    ];

    private readonly ILogger<YoutubeService> _logger;
    private readonly YtdlpOptions _options;
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly VideoRepository _videoRepository;
    private readonly VideoFileRepository _videoFileRepository;
    private readonly ImageFileRepository _imageFileRepository;
    private readonly PreferencesRepository _preferencesRepository;
    private readonly IClock _clock;
    private readonly NpgsqlConnection _connection;
    private readonly ISponsorBlockClient _sponsorBlockClient;
    private readonly SponsorBlockSegmentRepository _segmentRepository;
    private readonly YtdlpWrapper _ytdlpWrapper;
    private readonly WebVideoTextTracksService _webVideoTextTracksService;

    private static string VideoFormat { get; } = string.Join(',', VideoFormats);

    public YoutubeService(
        ILogger<YoutubeService> logger,
        IOptionsMonitor<YtdlpOptions> optionsMonitor,
        ChannelRepository channelRepository,
        VideoRepository videoRepository,
        VideoFileRepository videoFileRepository,
        ImageFileRepository imageFileRepository,
        IClock clock,
        LibraryRepository libraryRepository,
        NpgsqlConnection connection,
        PreferencesRepository preferencesRepository,
        ISponsorBlockClient sponsorBlockClient,
        SponsorBlockSegmentRepository segmentRepository,
        YtdlpWrapper ytdlpWrapper,
        WebVideoTextTracksService webVideoTextTracksService)
    {
        _logger = logger;
        _options = optionsMonitor.CurrentValue;
        _channelRepository = channelRepository;
        _videoRepository = videoRepository;
        _clock = clock;
        _libraryRepository = libraryRepository;
        _connection = connection;
        _preferencesRepository = preferencesRepository;
        _sponsorBlockClient = sponsorBlockClient;
        _segmentRepository = segmentRepository;
        _ytdlpWrapper = ytdlpWrapper;
        _webVideoTextTracksService = webVideoTextTracksService;
        _imageFileRepository = imageFileRepository;
        _videoFileRepository = videoFileRepository;
    }

    public async ValueTask Index(
        string url,
        Guid libraryId,
        Guid userId,
        DirectoryInfo tempDirectory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var cookieFilepath = await CreateCookieFile(libraryId, tempDirectory, cancellationToken);

        var youtube = new YoutubeDL
        {
            YoutubeDLPath = _options.YtdlpPath,
            FFmpegPath = _options.FfmpegPath,
        };

        var data = await _ytdlpWrapper.FetchUnknownUrlData(url, cookieFilepath, cancellationToken);
        var channel = await GetChannel(libraryId, userId, data, transaction);

        if (data.ResultType is MetadataType.Video)
        {
            await IndexVideo(
                url,
                channel,
                libraryId,
                userId,
                data,
                transaction,
                tempDirectory,
                cancellationToken);
        }
        else if (data.ResultType is MetadataType.Playlist)
        {
            await IndexChannel(
                channel,
                data,
                youtube,
                cookieFilepath,
                cancellationToken);
        }
        else
        {
            throw new InvalidOperationException(
                $"Unexpected metadata type when downloading video: {data.ResultType}");
        }

        await transaction.CommitWithRetries(_logger, cancellationToken);
    }

    private async ValueTask<ChannelEntity> GetChannel(
        Guid libraryId,
        Guid userId,
        VideoData data,
        NpgsqlTransaction transaction)
    {
        var library = await _libraryRepository.GetAsync(libraryId, userId, transaction);
        var youtubeChannelId = data.ChannelID;

        var channel = await _channelRepository.FindByExternalId(youtubeChannelId, userId, Access.Read, transaction);
        if (channel is null)
        {
            _logger.LogDebug("Creating new channel {ChannelName} ({ChannelExternalId})", data.Channel, youtubeChannelId);

            var channelId = await _channelRepository.AddAsync(
                new ChannelEntity
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    OwnerId = userId,
                    Name = data.Channel,
                    StoragePath = library.StoragePath,
                    ExternalId = youtubeChannelId,
                    ExternalUrl = data.ChannelUrl,
                    Availability = ExternalAvailability.Public,
                },
                transaction);

            channel = await _channelRepository.GetAsync(channelId!.Value, userId, transaction);

            channel.StoragePath = Path.Combine(library.StoragePath, $"channel_{channel.Id}");
            await _channelRepository.UpdateAsync(channel, transaction);
            await _channelRepository.AddToLibrary(libraryId, channel.Id, transaction);

            Directory.CreateDirectory(channel.StoragePath);
        }

        return channel;
    }

    private async ValueTask IndexVideo(
        string url,
        ChannelEntity channel,
        Guid libraryId,
        Guid userId,
        VideoData videoData,
        NpgsqlTransaction transaction,
        DirectoryInfo directory,
        CancellationToken cancellationToken,
        VideoType? type = null)
    {
        var cookieFilepath = await CreateCookieFile(libraryId, directory, cancellationToken);

        var youtubeVideoId = videoData.ID;
        _logger.LogDebug("Indexing video {VideoExternalId}", youtubeVideoId);

        var video = await _videoRepository.FindByExternalId(youtubeVideoId, userId, Access.Read, transaction);
        var existingVideo = video is not null;

        if (video is null)
        {
            var publishedAt = videoData.ReleaseTimestamp ?? videoData.Timestamp;
            var publishedInstant = Instant.FromDateTimeUtc(publishedAt!.Value);
            var duration = Period.FromSeconds((long)Math.Truncate(videoData.Duration!.Value));
            var availability = videoData.Availability switch
            {
                Public or Unlisted => ExternalAvailability.Public,
                Private or PremiumOnly or SubscriberOnly or NeedsAuth => ExternalAvailability.Private,
                _ => throw new ArgumentOutOfRangeException(nameof(videoData.Availability),
                    videoData.Availability, "Unexpected availability value"),
            };

            type ??= videoData switch
            {
                { LiveStatus: not (LiveStatus.None or LiveStatus.NotLive) } => VideoType.Livestream,
                _ => VideoType.Video,
            };

            var videoId = await _videoRepository.AddAsync(
                new VideoEntity
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    OwnerId = userId,
                    Name = videoData.Title,
                    Type = type,
                    Description = videoData.Description ?? string.Empty,
                    Categories = videoData.Categories ?? [],
                    Tags = videoData.Tags ?? [],
                    ViewCount = videoData.ViewCount,
                    LikeCount = videoData.LikeCount,
                    ChannelId = channel.Id,
                    StoragePath = channel.StoragePath,
                    ExternalId = youtubeVideoId,
                    ExternalUrl = videoData.WebpageUrl,
                    PublishedAt = publishedInstant,
                    RefreshedAt = _clock.GetCurrentInstant(),
                    Availability = availability,
                    Duration = duration,
                    TotalCount = 0,
                },
                transaction);

            video = await _videoRepository.GetAsync(videoId!.Value, userId, transaction);

            video.StoragePath = Path.Combine(channel.StoragePath, $"video_{video.Id}");
            await _videoRepository.UpdateAsync(video, transaction);

            Directory.CreateDirectory(video.StoragePath);
        }

        var segments = await _sponsorBlockClient.GetSegmentsPrivacy(video.ExternalId, cancellationToken);
        var existingSegments = await _segmentRepository.GetForVideo(video.Id, userId, transaction);
        foreach (var segment in segments)
        {
            var existingSegment = existingSegments.SingleOrDefault(entity => entity.ExternalId == segment.Id);
            if (existingSegment is not null)
            {
                continue;
            }

            await _segmentRepository.AddAsync(
                new SponsorBlockSegmentEntity
                {
                    CreatedByUserId = userId,
                    VideoId = video.Id,
                    ExternalId = segment.Id,
                    StartTime = segment.StartTime,
                    EndTime = segment.EndTime,
                    Category = segment.Category,
                    Action = segment.Action,
                    Description = segment.Description
                },
                transaction);
        }

        var preferences = await _preferencesRepository.GetEffectiveForChannel(libraryId, channel.Id, userId, cancellationToken);

        var files = await _videoRepository.GetFilesAsync(video.Id, userId, transaction);
        foreach (var file in files.Where(file => file.DownloadedAt is null).ToArray())
        {
            _logger.LogDebug("Deleting indexed but not downloaded video file {VideoFileId}", file.Id);
            await _videoFileRepository.DeleteAsync(file, transaction);
            files.Remove(file);
        }

        var videoFormats = new Dictionary<string, FormatData>();

        foreach (var format in VideoFormats)
        {
            var data = await _ytdlpWrapper.FetchVideoFormatData(url, format, cookieFilepath, preferences?.PlayerClient, cancellationToken);
            if (data is not { ResultType: MetadataType.Video })
            {
                _logger.LogError("Expected video, received {MetadataType} with data {VideoData}", data.ResultType, data);
                throw new Exception("Unexpected result type");
            }

            var formatIds = data.FormatID.Split('+');
            var formats = formatIds
                .Select(formatId => data.Formats.Single(formatData => formatData.FormatId == formatId))
                .ToArray();

            var videoFormat = formats.Single(formatData => formatData.Resolution is not "audio only");
            videoFormats.Add(format, videoFormat);
        }

        var distinctFormats = videoFormats.DistinctBy(pair => pair.Value.FormatId).ToArray();
        _logger.LogDebug("Selected {DistinctCount} distinct video formats from {Count}", distinctFormats.Length, videoFormats.Count);
        foreach (var format in videoFormats.Keys.Except(distinctFormats.Select(pair => pair.Key)))
        {
            _logger.LogDebug("Skipped format filter {FormatFilter}", format);
        }

        foreach (var (format, videoFormat) in distinctFormats)
        {
            using var scope = _logger.BeginScope("{FormatId}", videoFormat.FormatId);
            _logger.LogDebug("Selected format filter {FormatFilter}", format);

            var containerType = VideoContainerType.FromName(videoFormat.Extension);

            var file = files.SingleOrDefault(file =>
                file.Width == videoFormat.Width &&
                Math.Round(file.Framerate) == (decimal)Math.Round(videoFormat.FrameRate!.Value));

            if (file is null)
            {
                _logger.LogDebug("Could not find existing file for filter {FormatFilter}", format);

                var fileId = await _videoFileRepository.AddAsync(
                    new VideoFileEntity
                    {
                        CreatedByUserId = userId,
                        ModifiedByUserId = userId,
                        OwnerId = userId,
                        VideoId = video.Id,
                        StoragePath = $"video_{videoFormat.Height}.{containerType.Name}",
                        Type = containerType,
                        Width = videoFormat.Width!.Value,
                        Height = videoFormat.Height!.Value,
                        Framerate = (decimal)videoFormat.FrameRate!.Value,
                    },
                    transaction);

                file = await _videoFileRepository.GetAsync(fileId!.Value, userId, transaction);
            }
            else
            {
                _logger.LogDebug("Found existing file {FileId} for filter {FormatFilter}", file.Id, format);
            }

            _logger.LogDebug("Video file {VideoFile}", file);
        }

        if (videoData.Chapters is { Length: > 0 })
        {
            var chaptersFilePath = video.GetChaptersFilePath();
            _logger.LogDebug("Writing video chapters to {ChaptersFilePath}", chaptersFilePath);

            await using var chapterFile = File.Create(chaptersFilePath);
            var cues = videoData.Chapters.Select(TextTrackCue.FromYouTubeChapter);
            await _webVideoTextTracksService.Write(chapterFile, cues, cancellationToken);
        }

        var thumbnail = videoData
            .Thumbnails
            .Where(data => new UriBuilder(data.Url).Query.StartsWith("?sqp") && data.Width.HasValue)
            .OrderByDescending(data => data.Width)
            .FirstOrDefault() ?? videoData.Thumbnails.OrderByDescending(data => data.Width).First();

        await _ytdlpWrapper.DownloadThumbnail(thumbnail.Url, video.StoragePath, cookieFilepath, cancellationToken);
        var thumbnails = Directory.EnumerateFiles(video.StoragePath, "thumbnail.*").ToArray();
        if (thumbnails is [var thumbnailPath])
        {
            var imageFileId = await _imageFileRepository.AddAsync(
                new()
                {
                    CreatedByUserId = userId,
                    StoragePath = Path.GetFileName(thumbnailPath),
                    Type = ImageType.Thumbnail,
                    Width = thumbnail.Width!.Value,
                    Height = thumbnail.Height!.Value
                },
                transaction);

            await _imageFileRepository.LinkToVideoAsync(imageFileId!.Value, video.Id, userId, transaction);
        }
        else if (thumbnails is not [])
        {
            throw new Exception("Multiple thumbnails");
        }
    }

    private async ValueTask IndexChannel(
        ChannelEntity channel,
        VideoData videoData,
        YoutubeDL youtube,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Indexing channel");

        _logger.LogDebug("Downloading thumbnails for channel {ChannelId}", channel.Id);
        await youtube.RunWithOptions(
            videoData.ChannelUrl,
            new OptionSet
            {
                Output = "thumbnail:thumbnail.%(ext)s",
                Paths = $"{channel.StoragePath}",
                SkipDownload = true,
                LimitRate = _options.LimitRate,
                Cookies = cookieFilepath,
                CookiesFromBrowser = _options.CookiesFromBrowser,
                WriteAllThumbnails = true,
                PlaylistItems = "0",
            },
            cancellationToken);
    }

    public async ValueTask ScanChannel(
        Guid libraryId,
        Guid channelId,
        bool allVideos,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

        await ScanChannelCore(libraryId, channelId, allVideos, false, userId, taskRepository, taskRunId, transaction, tempDirectory, cancellationToken);

        await transaction.CommitWithRetries(_logger, cancellationToken);
    }

    public async ValueTask ScanSubscriptions(
        Guid libraryId,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var channels = await _channelRepository.GetSubscribedForLibrary(libraryId, userId, transaction);

        await taskRepository.InitializeTaskProgress(taskRunId, channels.Count);
        _logger.LogDebug("Found {Count} subscribed channels", channels.Count);

        foreach (var (index, channel) in channels.Index())
        {
            await ScanChannelCore(libraryId, channel.Id, false, true, userId, taskRepository, taskRunId, transaction, tempDirectory, cancellationToken, false);
            await taskRepository.UpdateProgress(taskRunId, index + 1);
        }

        await transaction.CommitWithRetries(_logger, cancellationToken);
    }

    public async ValueTask ScanSponsorBlockSegments(
        Guid libraryId,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var videosWithoutSegments = await _videoRepository.GetWithoutSegments(userId, libraryId, transaction);
        await taskRepository.InitializeTaskProgress(taskRunId, videosWithoutSegments.Count);

        foreach (var (index, videoId) in videosWithoutSegments.Index())
        {
            var segments = await _sponsorBlockClient.GetSegmentsPrivacy(videoId.ExternalId, cancellationToken);
            foreach (var segment in segments)
            {
                await _segmentRepository.AddAsync(
                    new SponsorBlockSegmentEntity
                    {
                        CreatedByUserId = userId,
                        VideoId = videoId.Id,
                        ExternalId = segment.Id,
                        StartTime = segment.StartTime,
                        EndTime = segment.EndTime,
                        Category = segment.Category,
                        Action = segment.Action,
                        Description = segment.Description
                    },
                    transaction);
            }

            await taskRepository.UpdateProgress(taskRunId, index + 1);
        }

        await transaction.CommitWithRetries(_logger, cancellationToken);
    }

    private async ValueTask ScanChannelCore(
        Guid libraryId,
        Guid channelId,
        bool allVideos,
        bool breakOnExisting,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        NpgsqlTransaction transaction,
        DirectoryInfo directory,
        CancellationToken cancellationToken,
        bool reportProgress = true)
    {
        var cookiesFilepath = await CreateCookieFile(libraryId, directory, cancellationToken);

        var channel = await _channelRepository.GetAsync(channelId, userId, transaction);
        var preferences = await _preferencesRepository.GetEffectiveForChannel(libraryId, channelId, userId, cancellationToken);

        var types = new List<(int Count, VideoType Type)>();

        if (preferences is null)
        {
            types.Add((5, VideoType.Video));
        }
        else if (preferences.VideosCount is { } videosCount)
        {
            types.Add((videosCount, VideoType.Video));
        }

        if (preferences?.LiveStreamsCount is { } liveStreamsCount)
        {
            types.Add((liveStreamsCount, VideoType.Livestream));
        }

        if (preferences?.ShortsCount is { } shortsCount)
        {
            types.Add((shortsCount, VideoType.Short));
        }

        var playlists = new Dictionary<VideoType, VideoData>();
        foreach (var (count, type) in types)
        {
            var playlistData = await _ytdlpWrapper.FetchPlaylistEntryUrls(
                $"{channel.ExternalUrl}/{type.Tab}",
                allVideos ? null : count,
                cookiesFilepath,
                cancellationToken);

            playlists.Add(type, playlistData);
        }

        if (reportProgress)
        {
            await taskRepository.InitializeTaskProgress(taskRunId, playlists.SelectMany(pair => pair.Value.Entries).Count());
        }

        var indexOffset = 0;
        foreach (var (type, playlistData) in playlists)
        {
            foreach (var (index, video) in playlistData.Entries.Index())
            {
                var existing = await _videoRepository.FindByExternalUrl(video.Url, userId, Access.Read, transaction);
                if (existing is not null && breakOnExisting)
                {
                    _logger.LogDebug("Found existing video for {VideoExternalUrl}, stopping channel scan", existing.ExternalUrl);
                    if (reportProgress)
                    {
                        await taskRepository.UpdateProgress(taskRunId, playlistData.Entries.Length + indexOffset);
                    }

                    break;
                }

                var videoResult = await _ytdlpWrapper.FetchVideoData(video.Url, cookiesFilepath, cancellationToken);
                if (!videoResult.Success)
                {
                    _logger.LogWarning("Skipping video during channel scan - {ErrorMessage}", string.Join(Environment.NewLine, videoResult.ErrorOutput));
                    continue;
                }

                await IndexVideo(video.Url, channel, libraryId, userId, videoResult.Data, transaction, directory, cancellationToken, type);

                if (reportProgress)
                {
                    await taskRepository.UpdateProgress(taskRunId, index + 1 + indexOffset);
                }
            }

            indexOffset += playlistData.Entries.Length;
        }
    }

    public async ValueTask DownloadVideo(
        Guid libraryId,
        Guid videoId,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var cookieFilepath = await CreateCookieFile(libraryId, tempDirectory, cancellationToken);

        var video = await _videoRepository.GetAsync(videoId, userId, transaction);
        var preferences = await _preferencesRepository.GetEffectiveForVideo(libraryId, videoId, userId, cancellationToken);
        var youtubeClient = preferences?.PlayerClient is { } client
            ? new MultiValue<string>($"youtube:player_client={client.Name}")
            : null;

        var youtube = new YoutubeDL
        {
            YoutubeDLPath = _options.YtdlpPath,
            FFmpegPath = _options.FfmpegPath,
        };

        _logger.LogInformation("Getting video metadata for {VideoUrl}", video.ExternalUrl);

        var videoData = await youtube.RunVideoDataFetch(
            video.ExternalUrl,
            cancellationToken,
            true,
            false,
            new OptionSet
            {
                Cookies = cookieFilepath,
                CookiesFromBrowser = _options.CookiesFromBrowser,
                Format = VideoFormat,
                ExtractorArgs = youtubeClient,
                CustomOptions = [new Option<string>("-t") { Value = "mp4" }]
            });

        if (!videoData.Success)
        {
            throw new Exception(string.Join(Environment.NewLine, videoData.ErrorOutput));
        }

        if (videoData.Data.ResultType is not MetadataType.Video)
        {
            throw new InvalidOperationException(
                $"Unexpected metadata type when downloading video: {videoData.Data.ResultType}");
        }

        var files = await _videoRepository.GetFilesAsync(videoId, userId, transaction);

        var selectedFormats = await _ytdlpWrapper.SelectFormats(
            video.ExternalUrl,
            VideoFormats,
            cookieFilepath,
            preferences?.PlayerClient,
            cancellationToken);

        var totalSize = selectedFormats
            .SelectMany(tuple => tuple.Formats)
            .Sum(format => (decimal?)(format.FileSize ?? format.ApproximateFileSize));

        if (totalSize.HasValue)
        {
            await taskRepository.InitializeTaskProgress(taskRunId, totalSize.Value);
        }

        var targetDirectory = video.GetDirectoryPath();
        Directory.CreateDirectory(targetDirectory);

        var sizeOffset = 0m;

        foreach (var (selectedFormat, formats) in selectedFormats)
        {
            foreach (var formatData in formats)
            {
                _logger.LogDebug("Selected format {FormatData}", formatData);
            }

            var videoFormat = formats.Single(format => !format.Resolution.Contains("audio only", StringComparison.OrdinalIgnoreCase));
            var containerType = VideoContainerType.FromName(videoFormat.Extension);
            var videoFile = files.Single(file => file.Type == containerType && file.Height == videoFormat.Height!.Value);

            var size = formats.Sum(format => (decimal?)(format.FileSize ?? format.ApproximateFileSize));

            _logger.LogInformation(
                "Selected format with size {VideoSize} MB",
                size.HasValue ? Math.Round(size.Value / 1024 / 1024, 2) : null);

            var fileName = videoFile.StoragePath;
            if (Directory.EnumerateFiles(targetDirectory, $"{video.Id}.*").Any(file => file.EndsWith(fileName)))
            {
                _logger.LogInformation("Video already exists {VideoUrl}", video.ExternalUrl);
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            youtube.OutputFolder = tempDirectory.FullName;
            youtube.OutputFileTemplate = $"{Path.GetFileNameWithoutExtension(fileName)}.%(ext)s";

            var outputProgress = new Progress<string>(s => _logger.LogDebug("yt-dlp output: {Output}", s));
            var mergeFormat = videoFile.Type.Name switch
            {
                VideoContainerType.Names.Mp4 => DownloadMergeFormat.Mp4,
                VideoContainerType.Names.WebM => DownloadMergeFormat.Webm,
                _ => DownloadMergeFormat.Unspecified,
            };

            _logger.LogInformation("Downloading video {VideoUrl} to {Directory}", video.ExternalUrl, tempDirectory.FullName);
            var downloadTask = youtube.RunVideoDownload(
                video.ExternalUrl,
                format: selectedFormat,
                mergeFormat,
                VideoRecodeFormat.None,
                cancellationToken,
                null,
                outputProgress,
                new OptionSet
                {
                    LimitRate = _options.LimitRate,
                    Cookies = cookieFilepath,
                    CookiesFromBrowser = _options.CookiesFromBrowser,
                    WriteSubs = true,
                    NoWriteAutoSubs = true,
                    SubFormat = "vtt",
                    SubLangs = "all,-live_chat",
                    NoPart = true,
                    EmbedChapters = true,
                    ExtractorArgs = youtubeClient,
                    CustomOptions =
                    [
                        new Option<string>("-o") { Value = $"subtitle:{Path.Combine(tempDirectory.FullName, "subtitles.%(ext)s")}" },
                    ]
                });

            string? storagePath = null;

            var timestamp = Stopwatch.GetTimestamp();
            var fileSize = 0L;
            var pollingDelay = TimeSpan.FromSeconds(2);

            while (!downloadTask.IsCompleted || storagePath is null)
            {
                var startTimestamp = Stopwatch.GetTimestamp();

                storagePath =
                    tempDirectory.EnumerateFiles($"{Path.GetFileNameWithoutExtension(fileName)}*.*").ToArray() switch
                    {
                        [var tempFile] => tempFile.FullName,
                        _ => null,
                    };

                if (storagePath is null)
                {
                    var remaining = pollingDelay - Stopwatch.GetElapsedTime(startTimestamp);
                    if (remaining < TimeSpan.Zero)
                    {
                        continue;
                    }

                    await Task.Delay(remaining, cancellationToken);
                    continue;
                }

                var newTimestamp = Stopwatch.GetTimestamp();
                var newFileSize = new FileInfo(storagePath).Length;

                var elapsedSeconds = Stopwatch.GetElapsedTime(timestamp, newTimestamp).TotalSeconds;
                var sizeDelta = newFileSize - fileSize;

                _logger.LogInformation(
                    "Downloading video at {DownloadSpeed} kb/s {FilePath}",
                    Math.Round(sizeDelta / elapsedSeconds / 1024, 0),
                    storagePath);

                timestamp = newTimestamp;
                fileSize = newFileSize;

                if (totalSize.HasValue && size.HasValue)
                {
                    await taskRepository.UpdateProgress(taskRunId, fileSize + sizeOffset);
                }

                var remaining2 = pollingDelay - Stopwatch.GetElapsedTime(startTimestamp);
                if (remaining2 < TimeSpan.Zero)
                {
                    continue;
                }

                await Task.Delay(remaining2, cancellationToken);
            }

            var downloadResult = await downloadTask;

            if (downloadResult.Success)
            {
                _logger.LogInformation("Downloaded video {VideoUrl}", video.ExternalUrl);

                videoFile.ModifiedAt = _clock.GetCurrentInstant();
                videoFile.ModifiedByUserId = userId;
                videoFile.DownloadedAt = _clock.GetCurrentInstant();
                videoFile.DownloadedByUserId = userId;
                await _videoFileRepository.UpdateAsync(videoFile, transaction);

                sizeOffset += size ?? 0;
            }
            else
            {
                _logger.LogWarning("Failed to download video {VideoUrl}", video.ExternalUrl);
                throw new Exception(string.Join(Environment.NewLine, downloadResult.ErrorOutput));
            }
        }

        if (cookieFilepath is not null && File.Exists(cookieFilepath))
        {
            File.Delete(cookieFilepath);
        }

        foreach (var tempFile in tempDirectory.EnumerateFiles())
        {
            var targetFilePath = Path.Combine(targetDirectory, tempFile.Name);
            _logger.LogDebug("Moving file from {SourcePath} to {TargetPath}", tempFile.FullName, targetFilePath);
            tempFile.MoveTo(targetFilePath);
        }

        await transaction.CommitWithRetries(_logger, cancellationToken);
    }

    private async ValueTask<string?> CreateCookieFile(
        Guid libraryId,
        DirectoryInfo directory,
        CancellationToken cancellationToken)
    {
        var cookie = await _connection.QuerySingleOrDefaultAsync<LibraryCookieEntity>(new CommandDefinition(
            $"""
             SELECT id AS Id,
                    created_at AS CreatedAt,
                    created_by_user_id AS CreatedByUserId,
                    modified_at AS ModifiedAt,
                    modified_by_user_id AS ModifiedByUserId,
                    domain AS Domain,
                    cookie AS Cookie
             FROM media.library_external_cookies
             WHERE id = @{nameof(libraryId)} AND domain = 'youtube.com';
             """,
            new { libraryId }));

        if (cookie?.Cookie is null)
        {
            _logger.LogDebug("No cookies found, not creating cookie file");
            return null;
        }

        var cookieFilepath = Path.Combine(directory.FullName, "cookie.jar");
        await using var cookieFile = File.Create(cookieFilepath);
        await using var writer = new StreamWriter(cookieFile);
        await writer.WriteAsync(new StringBuilder(cookie.Cookie), cancellationToken);

        return cookieFilepath;
    }
}
