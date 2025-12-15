using System;
using System.Collections.Generic;
using System.Data;
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
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration;
using Tubeshade.Server.Pages.Videos;
using Tubeshade.Server.Services.Ffmpeg;
using YoutubeDLSharp.Metadata;
using static YoutubeDLSharp.Metadata.Availability;

namespace Tubeshade.Server.Services;

public sealed class YoutubeService
{
    private const string UpscalingFilter = "[url!*='xtags=sr%3D1']";

    internal const int DefaultVideoCount = 5;
    internal static readonly string[] DefaultVideoFormats =
    [
        $"bv{UpscalingFilter}+(ba[format_note*=original]/ba)/best{UpscalingFilter}",
        $"bv*[height<=720]{UpscalingFilter}+(ba[format_note*=original]/ba)"
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
    private readonly IYtdlpWrapper _ytdlpWrapper;
    private readonly WebVideoTextTracksService _webVideoTextTracksService;
    private readonly TaskService _taskService;
    private readonly SponsorBlockService _sponsorBlockService;
    private readonly FfmpegService _ffmpegService;

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
        IYtdlpWrapper ytdlpWrapper,
        WebVideoTextTracksService webVideoTextTracksService,
        TaskService taskService,
        SponsorBlockService sponsorBlockService,
        FfmpegService ffmpegService)
    {
        _logger = logger;
        _options = optionsMonitor.CurrentValue;
        _channelRepository = channelRepository;
        _videoRepository = videoRepository;
        _clock = clock;
        _libraryRepository = libraryRepository;
        _connection = connection;
        _preferencesRepository = preferencesRepository;
        _ytdlpWrapper = ytdlpWrapper;
        _webVideoTextTracksService = webVideoTextTracksService;
        _taskService = taskService;
        _sponsorBlockService = sponsorBlockService;
        _ffmpegService = ffmpegService;
        _imageFileRepository = imageFileRepository;
        _videoFileRepository = videoFileRepository;
    }

    public async ValueTask<UrlIndexingResult> Index(
        string url,
        Guid libraryId,
        Guid userId,
        Guid? videoId,
        Guid? channelId,
        DirectoryInfo tempDirectory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var cookieFilepath = await CreateCookieFile(libraryId, tempDirectory, cancellationToken);

        var data = await _ytdlpWrapper.FetchUnknownUrlData(url, cookieFilepath, cancellationToken);

        var channel = videoId is not null && channelId is not null
            ? await _channelRepository.GetAsync(channelId.Value, userId, transaction)
            : await GetChannel(libraryId, userId, data, transaction);

        var result = new UrlIndexingResult { ChannelId = channel.Id };

        if (data.ResultType is MetadataType.Video)
        {
            result.VideoId = await IndexVideo(
                url,
                videoId,
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
            await IndexChannel(channel, data, cookieFilepath, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException(
                $"Unexpected metadata type when downloading video: {data.ResultType}");
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async ValueTask<ChannelEntity> GetChannel(
        Guid libraryId,
        Guid userId,
        VideoData data,
        NpgsqlTransaction transaction)
    {
        if (data.ChannelId is not { } youtubeChannelId ||
            data.Channel is not { } channelName ||
            data.ChannelUrl is not { } channelUrl)
        {
            if (data.Availability is null)
            {
                throw new InvalidOperationException("Cannot create a channel from an unavailable video");
            }

            throw new InvalidOperationException("Missing channel details");
        }

        var channel = await _channelRepository.FindByExternalId(youtubeChannelId, userId, Access.Read, transaction);
        if (channel is not null)
        {
            return channel;
        }

        var library = await _libraryRepository.GetAsync(libraryId, userId, transaction);
        _logger.CreatingChannel(channelName, youtubeChannelId);

        var channelId = await _channelRepository.AddAsync(
            new ChannelEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = library.OwnerId,
                Name = channelName,
                StoragePath = library.StoragePath,
                ExternalId = youtubeChannelId,
                ExternalUrl = channelUrl,
                Availability = ExternalAvailability.Public,
            },
            transaction);

        channel = await _channelRepository.GetAsync(channelId!.Value, userId, transaction);

        channel.StoragePath = Path.Combine(library.StoragePath, $"channel_{channel.Id}");
        await _channelRepository.UpdateAsync(channel, transaction);
        await _channelRepository.AddToLibrary(libraryId, channel.Id, transaction);

        Directory.CreateDirectory(channel.StoragePath);

        return channel;
    }

    private async ValueTask<Guid> IndexVideo(
        string url,
        Guid? videoId,
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

        var library = await _libraryRepository.GetAsync(libraryId, userId, transaction);
        var youtubeVideoId = videoData.Id;
        _logger.IndexingVideo(youtubeVideoId);

        var video = await _videoRepository.FindByExternalId(youtubeVideoId, userId, Access.Read, transaction);
        var isNewVideo = video is null;

        var currentTime = _clock.GetCurrentInstant();
        var availability = videoData.Availability switch
        {
            Public or Unlisted => ExternalAvailability.Public,
            Private or PremiumOnly or SubscriberOnly or NeedsAuth => ExternalAvailability.Private,
            null => ExternalAvailability.NotAvailable,
            _ => throw new ArgumentOutOfRangeException(
                nameof(videoData.Availability),
                videoData.Availability,
                "Unexpected availability value"),
        };

        if (availability == ExternalAvailability.NotAvailable)
        {
            if (video is null)
            {
                throw new InvalidOperationException("Video is not available");
            }

            video.ModifiedAt = currentTime;
            video.ModifiedByUserId = userId;
            video.RefreshedAt = currentTime;
            video.Availability = availability;

            await _videoRepository.UpdateAsync(video, transaction);
            return video.Id;
        }

        // todo: livestreams, as well as videos from some sites besides YouTube, don't have duration
        var duration = Period.FromSeconds(
            (long)Math.Truncate(videoData.Duration ?? throw new InvalidOperationException("Video is missing duration")));

        if (video is null)
        {
            var publishedAt = videoData.ReleaseTimestamp ?? videoData.Timestamp
                ?? throw new InvalidOperationException("Video is missing publish date");
            var publishedInstant = Instant.FromUnixTimeSeconds(publishedAt);

            type ??= videoData switch
            {
                { LiveStatus: not (LiveStatus.None or LiveStatus.NotLive) } => VideoType.Livestream,
                _ => VideoType.Video,
            };

            videoId = await _videoRepository.AddAsync(
                new VideoEntity
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    OwnerId = library.OwnerId,
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
                    ExternalUrl = videoData.WebpageUrl ?? throw new InvalidOperationException("Missing video url"),
                    PublishedAt = publishedInstant,
                    RefreshedAt = currentTime,
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
        else
        {
            video.ModifiedAt = currentTime;
            video.ModifiedByUserId = userId;
            video.Name = videoData.Title;
            video.Description = videoData.Description ?? string.Empty;
            video.Categories = videoData.Categories ?? [];
            video.Tags = videoData.Tags ?? [];
            video.ViewCount = videoData.ViewCount;
            video.LikeCount = videoData.LikeCount;
            video.RefreshedAt = currentTime;
            video.Availability = availability;
            video.Duration = duration;

            await _videoRepository.UpdateAsync(video, transaction);
        }

        await _sponsorBlockService.UpdateVideoSegments(video, userId, transaction, cancellationToken);

        var preferences = await _preferencesRepository.GetEffectiveForChannel(libraryId, channel.Id, userId, cancellationToken) ?? new();
        preferences.ApplyDefaults();

        var files = await _videoRepository.GetFilesAsync(video.Id, userId, transaction);
        foreach (var file in files.Where(file => file.DownloadedAt is null).ToArray())
        {
            _logger.DeletingNotDownloadedFile(file.Id);
            await _videoFileRepository.DeleteAsync(file, transaction);
            files.Remove(file);
        }

        var videoFormats = new Dictionary<string, FormatData>();
        var formatSelectors = preferences.Formats is { Length: > 0 } preferredFormats
            ? preferredFormats
            : DefaultVideoFormats;

        foreach (var format in formatSelectors)
        {
            var data = await _ytdlpWrapper.FetchVideoFormatData(
                url,
                format,
                cookieFilepath,
                preferences.PlayerClient,
                availability != ExternalAvailability.Public,
                cancellationToken);

            if (data.Formats is null or [])
            {
                _logger.NoFormats(format);
                continue;
            }

            var formatIds = data.FormatId!.Split('+');
            var formats = formatIds
                .Select(formatId => data.Formats.Single(formatData => formatData.FormatId == formatId))
                .ToArray();

            var videoFormat = formats.Single(formatData => formatData.Resolution is not "audio only");
            videoFormats.Add(format, videoFormat);
        }

        var distinctFormats = videoFormats.DistinctBy(pair => pair.Value.FormatId).ToArray();
        _logger.SelectedFormats(distinctFormats.Length, videoFormats.Count);
        foreach (var format in videoFormats.Keys.Except(distinctFormats.Select(pair => pair.Key)))
        {
            _logger.SkippedFormat(format);
        }

        foreach (var (format, videoFormat) in distinctFormats)
        {
            using var scope = _logger.BeginScope("{FormatId}", videoFormat.FormatId);
            _logger.SelectedFormat(format);

            var containerType = VideoContainerType.FromName(videoFormat.Extension);

            var matchingFiles = files
                .Where(file =>
                    file.Width == videoFormat.Width &&
                    Math.Round(file.Framerate) == (decimal)Math.Round(videoFormat.FrameRate!.Value))
                .ToArray();

            if (matchingFiles is [])
            {
                _logger.CreatingFileForFormat(format);

                var fileId = await _videoFileRepository.AddAsync(
                    new VideoFileEntity
                    {
                        CreatedByUserId = userId,
                        ModifiedByUserId = userId,
                        OwnerId = library.OwnerId,
                        VideoId = video.Id,
                        StoragePath = $"video_{videoFormat.Height}.{containerType.Name}",
                        Type = containerType,
                        Width = videoFormat.Width!.Value,
                        Height = videoFormat.Height!.Value,
                        Framerate = (decimal)videoFormat.FrameRate!.Value,
                    },
                    transaction);

                _ = fileId ?? throw new InvalidOperationException("Failed to create file");
            }
            else if (matchingFiles is [var file])
            {
                _logger.ExistingFileForFormat(file.Id, format);
            }
            else
            {
                _logger.ExistingFilesForFormat(format);
            }
        }

        var youtubeChapters = videoData.Chapters?.Select(TextTrackCue.FromYouTubeChapter).ToArray();
        var descriptionChapters = video.Description.TryExtractChapters(video.Duration, out var chaptersCues)
            ? chaptersCues
            : null;

        var chapterCues = (youtubeChapters, descriptionChapters) switch
        {
            ({ Length: > 0 } cues, null) => cues,
            (null, { Length: > 0 } cues) => cues,
            ({ Length: > 0 } youtube, { Length: > 0 } description) => youtube.Length >= description.Length
                ? youtube
                : description,
            _ => null,
        };

        if (chapterCues is { Length: > 0 })
        {
            var chaptersFilePath = video.GetChaptersFilePath();
            _logger.WritingVideoChapters(chaptersFilePath);

            await using var chapterFile = File.Create(chaptersFilePath);
            await _webVideoTextTracksService.Write(chapterFile, chapterCues, cancellationToken);
        }

        var existingThumbnail = await _imageFileRepository.FindVideoThumbnail(video.Id, userId, Access.Modify, transaction);
        var thumbnail = videoData.Thumbnails!.Single(thumbnail => thumbnail.Url == videoData.Thumbnail);
        if (existingThumbnail is not null && existingThumbnail.Width >= thumbnail.Width)
        {
            _logger.ExistingThumbnail();
        }
        else
        {
            var fileName = $"thumbnail_{video.Id:N}";
            await _ytdlpWrapper.DownloadThumbnail(thumbnail.Url, directory.FullName, fileName, cookieFilepath, cancellationToken);

            var thumbnails = directory.EnumerateFiles($"{fileName}.*").ToArray();
            if (thumbnails is [])
            {
                throw new Exception("Could not find downloaded thumbnail");
            }

            if (thumbnails is not [var thumbnailFile])
            {
                throw new Exception("Multiple thumbnails");
            }

            await CreateOrUpdateThumbnail(userId, video, thumbnailFile, existingThumbnail, transaction, cancellationToken);
        }

        if (!isNewVideo)
        {
            _logger.NotDownloadingExistingVideo();
            return video.Id;
        }

        if (preferences.DownloadVideos is null || preferences.DownloadVideos == DownloadVideos.None)
        {
            _logger.NotDownloadingDueToPreferences();
            return video.Id;
        }

        if (preferences.DownloadVideos == DownloadVideos.New)
        {
            var latest = await _videoRepository.GetLatestDownloadedVideo(userId, libraryId, channel.Id, transaction);
            if (latest is not null && latest.PublishedAt >= video.PublishedAt)
            {
                _logger.NotDownloadingOldVideo(latest.Id);
                return video.Id;
            }
        }
        else if (preferences.DownloadVideos != DownloadVideos.All)
        {
            throw new InvalidOperationException($"Unexpected download videos value '{preferences.DownloadVideos}'");
        }

        await _taskService.DownloadVideo(userId, libraryId, video.Id, transaction);
        return video.Id;
    }

    private async ValueTask CreateOrUpdateThumbnail(
        Guid userId,
        VideoEntity video,
        FileInfo file,
        ImageFileEntity? existing,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var response = await _ffmpegService.AnalyzeFile(file.FullName, cancellationToken);
        if (response.Streams is not [var stream])
        {
            throw new InvalidOperationException("Unexpected image file format");
        }

        if (stream is not { Width: { } width, Height: { } height })
        {
            throw new InvalidOperationException("Could not extract image resolution");
        }

        var destinationFileName = Path.Combine(video.StoragePath, file.Name);
        if (existing is not null && existing.Width >= width)
        {
            _logger.ExistingThumbnail();
            return;
        }

        if (existing is null)
        {
            _logger.CreatingThumbnail();

            var imageFileId = await _imageFileRepository.AddAsync(
                new()
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    StoragePath = file.Name,
                    Type = ImageType.Thumbnail,
                    Width = width,
                    Height = height
                },
                transaction);

            await _imageFileRepository.LinkToVideoAsync(imageFileId!.Value, video.Id, userId, transaction);
        }
        else
        {
            _logger.UpdatingExistingThumbnail(existing.Id);

            existing.ModifiedByUserId = userId;
            existing.StoragePath = file.Name;
            existing.Type = ImageType.Thumbnail;
            existing.Width = width;
            existing.Height = height;

            var count = await _imageFileRepository.UpdateAsync(existing, transaction);
            if (count is 0)
            {
                throw new InvalidOperationException("Failed to update existing thumbnail");
            }
        }

        file.MoveTo(destinationFileName, true);
    }

    private async ValueTask IndexChannel(
        ChannelEntity channel,
        VideoData videoData,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Indexing channel");

        _logger.LogDebug("Downloading thumbnails for channel {ChannelId}", channel.Id);

        await _ytdlpWrapper.DownloadChannelThumbnails(
            videoData.ChannelUrl ?? throw new InvalidOperationException("Missing channel Url"),
            channel.StoragePath,
            cookieFilepath,
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

        await transaction.CommitAsync(cancellationToken);
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

        await transaction.CommitAsync(cancellationToken);
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
        var preferences = await _preferencesRepository.GetEffectiveForChannel(libraryId, channelId, userId, cancellationToken) ?? new();
        preferences.ApplyDefaults();

        var types = new List<(int Count, VideoType Type)>();

        if (preferences.VideosCount is null)
        {
            types.Add((DefaultVideoCount, VideoType.Video));
        }
        else if (preferences.VideosCount is { } videosCount and not 0)
        {
            types.Add((videosCount, VideoType.Video));
        }

        if (preferences.LiveStreamsCount is { } liveStreamsCount and not 0)
        {
            types.Add((liveStreamsCount, VideoType.Livestream));
        }

        if (preferences.ShortsCount is { } shortsCount and not 0)
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
            await taskRepository.InitializeTaskProgress(taskRunId, playlists.SelectMany(pair => pair.Value.Entries ?? []).Count());
        }

        var indexOffset = 0;
        foreach (var (type, playlistData) in playlists)
        {
            if (playlistData.Entries is null)
            {
                throw new InvalidOperationException("Playlist is missing entries");
            }

            foreach (var (index, video) in playlistData.Entries.Index())
            {
                if (video.Url is null)
                {
                    throw new InvalidOperationException("Playlist entry is missing the Url");
                }

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

                await IndexVideo(video.Url, null, channel, libraryId, userId, videoResult.Data, transaction, directory, cancellationToken, type);

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
        var preferences = await _preferencesRepository.GetEffectiveForVideo(libraryId, videoId, userId, cancellationToken) ?? new();
        preferences.ApplyDefaults();

        var formatSelectors = preferences.Formats is { Length: > 0 } preferredFormats
            ? preferredFormats
            : DefaultVideoFormats;

        var files = await _videoRepository.GetFilesAsync(videoId, userId, transaction);

        var selectedFormats = await _ytdlpWrapper.SelectFormats(
            video.ExternalUrl,
            formatSelectors,
            cookieFilepath,
            preferences.PlayerClient,
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

            var videoFormat = formats.Single(format => !format.Resolution!.Contains("audio only", StringComparison.OrdinalIgnoreCase));
            var containerType = VideoContainerType.FromName(videoFormat.Extension);
            var videoFile = files.Single(file => file.Type == containerType && file.Height == videoFormat.Height!.Value);

            var size = formats.Sum(format => (decimal?)(format.FileSize ?? format.ApproximateFileSize));
            if (videoFile.DownloadedAt is not null)
            {
                _logger.LogInformation("Video file {VideoFileId} already exists", videoFile.Id);
                sizeOffset += size ?? 0;
                continue;
            }

            _logger.LogInformation(
                "Selected format with size {VideoSize} MB",
                size.HasValue ? Math.Round(size.Value / 1024 / 1024, 2) : null);

            var fileName = videoFile.StoragePath;

            var limitRate = _options.LimitRate;
            if (size.HasValue && _options.LimitMultiplier is { } multiplier)
            {
                var duration = (decimal)video.Duration.ToDuration().TotalSeconds;
                var bitrate = (long)Math.Round(size.Value * 8 * multiplier / duration, 0);
                if (limitRate is null || limitRate.Value > bitrate)
                {
                    limitRate = bitrate;
                }
            }

            _logger.LogInformation("Downloading video {VideoUrl} to {Directory}", video.ExternalUrl, tempDirectory.FullName);
            var downloadTask = _ytdlpWrapper.DownloadVideo(
                video.ExternalUrl,
                selectedFormat,
                videoFile.Type,
                tempDirectory.FullName,
                $"{Path.GetFileNameWithoutExtension(fileName)}.%(ext)s",
                cookieFilepath,
                limitRate,
                preferences.PlayerClient,
                cancellationToken);

            var timestamp = Stopwatch.GetTimestamp();
            var fileSize = 0L;
            var pollingDelay = TimeSpan.FromSeconds(2);

            while (!downloadTask.IsCompleted)
            {
                var startTimestamp = Stopwatch.GetTimestamp();

                var storagePath = tempDirectory
                        .EnumerateFiles($"{Path.GetFileNameWithoutExtension(fileName)}*.*")
                        .Where(file => file.Extension.ToLowerInvariant().TrimStart('.') is VideoContainerType.Names.Mp4 or VideoContainerType.Names.WebM)
                        .ToArray() switch
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

                timestamp = newTimestamp;
                fileSize = newFileSize;

                if (totalSize is { } total && size.HasValue)
                {
                    var rate = sizeDelta / (decimal)elapsedSeconds;
                    var remainingSize = total - (newFileSize + sizeOffset);
                    var remainingDuration = rate > 0
                        ? Period.FromNanoseconds((long)(remainingSize / rate * 1_000_000_000)).Normalize()
                        : null;

                    await taskRepository.UpdateProgress(taskRunId, fileSize + sizeOffset, rate, remainingDuration);
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

        if (video is not { IgnoredAt: null, IgnoredByUserId: null })
        {
            video.ModifiedByUserId = userId;
            video.IgnoredAt = null;
            video.IgnoredByUserId = null;

            var count = await _videoRepository.UpdateAsync(video, transaction);
            if (count < 1)
            {
                throw new InvalidOperationException("Failed to remove ignored status from video");
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async ValueTask Reindex(Guid libraryId, Guid userId, CancellationToken cancellationToken)
    {
        // we only read data at the start of the transaction, so ReadCommitted is ok
        await using var transaction = await _connection.OpenAndBeginTransaction(IsolationLevel.ReadCommitted, cancellationToken);

        foreach (var (videoId, channelId, videoUrl) in await _videoRepository.GetForReindex(libraryId, transaction))
        {
            await _taskService.IndexVideo(userId, libraryId, videoUrl, channelId, videoId, transaction);
        }

        await transaction.CommitAsync(cancellationToken);
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
