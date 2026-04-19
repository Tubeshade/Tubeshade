using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Pages.Videos;
using Tubeshade.Server.Services.Ffmpeg;
using Tubeshade.Server.Services.Migrations;
using YoutubeDLSharp.Metadata;
using static System.Data.IsolationLevel;
using static YoutubeDLSharp.Metadata.Availability;

namespace Tubeshade.Server.Services;

public sealed class YoutubeIndexingService
{
    private const string UpscalingFilter = "[url!*='xtags=sr%3D1']";

    internal const int DefaultVideoCount = 5;
    internal static readonly string[] DefaultVideoFormats =
    [
        $"bv{UpscalingFilter}+(ba[format_note*=original]/ba)/best{UpscalingFilter}",
        $"bv*[height<=720]{UpscalingFilter}+(ba[format_note*=original]/ba)"
    ];

    private readonly ILogger<YoutubeIndexingService> _logger;
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly VideoRepository _videoRepository;
    private readonly VideoFileRepository _videoFileRepository;
    private readonly ImageFileRepository _imageFileRepository;
    private readonly TrackFileRepository _trackFileRepository;
    private readonly PreferencesRepository _preferencesRepository;
    private readonly IClock _clock;
    private readonly NpgsqlConnection _connection;
    private readonly IYtdlpWrapper _ytdlpWrapper;
    private readonly WebVideoTextTracksService _webVideoTextTracksService;
    private readonly TaskService _taskService;
    private readonly SponsorBlockService _sponsorBlockService;
    private readonly FfmpegService _ffmpegService;
    private readonly ChannelService _channelService;
    private readonly VideoService _videoService;
    private readonly TrackFileService _trackFileService;

    public YoutubeIndexingService(
        ILogger<YoutubeIndexingService> logger,
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
        FfmpegService ffmpegService,
        ChannelService channelService,
        VideoService videoService,
        TrackFileRepository trackFileRepository,
        TrackFileService trackFileService)
    {
        _logger = logger;
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
        _channelService = channelService;
        _videoService = videoService;
        _trackFileRepository = trackFileRepository;
        _trackFileService = trackFileService;
        _imageFileRepository = imageFileRepository;
        _videoFileRepository = videoFileRepository;
    }

    public async ValueTask<UrlIndexingResult> Index(
        string url,
        Guid libraryId,
        Guid userId,
        Guid? channelId,
        DirectoryInfo tempDirectory,
        TaskSource source,
        CookiesService cookiesService,
        CancellationToken cancellationToken)
    {
        var cookieFilepath = await cookiesService.RefreshCookieFile();
        var data = await _ytdlpWrapper.FetchUnknownUrlData(url, cookieFilepath, cancellationToken);

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var channel = channelId is not null
            ? await _channelRepository.GetAsync(channelId.Value, userId, transaction)
            : await GetChannel(libraryId, userId, data, transaction);

        var result = new UrlIndexingResult { ChannelId = channel.Id };

        if (data.ResultType is MetadataType.Video)
        {
            result.VideoId = await IndexVideo(
                url,
                channel,
                libraryId,
                userId,
                data,
                transaction,
                tempDirectory,
                source, cookiesService,
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
        if (data.ChannelId is not { } externalId ||
            data.Channel is not { } name ||
            data.ChannelUrl is not { } externalUrl)
        {
            if (data.Availability is null)
            {
                throw new InvalidOperationException("Cannot create a channel from an unavailable video");
            }

            throw new InvalidOperationException("Missing channel details");
        }

        var channel = await _channelRepository.FindByExternalId(externalId, userId, Access.Read, transaction);
        if (channel is not null)
        {
            return channel;
        }

        var availability = ExternalAvailability.Public;
        return await _channelService.Create(libraryId, userId, name, externalId, externalUrl, availability, transaction);
    }

    private async ValueTask<Guid> IndexVideo(
        string url,
        ChannelEntity channel,
        Guid libraryId,
        Guid userId,
        VideoData videoData,
        NpgsqlTransaction transaction,
        DirectoryInfo directory,
        TaskSource source,
        CookiesService cookiesService,
        CancellationToken cancellationToken,
        VideoType? type = null)
    {
        var cookieFilepath = await cookiesService.RefreshCookieFile();

        var library = await _libraryRepository.GetAsync(libraryId, userId, transaction);
        var youtubeVideoId = videoData.Id;
        _logger.IndexingVideo(youtubeVideoId);
        _logger.VideoLiveStatus(youtubeVideoId, videoData.WasLive, videoData.IsLive, videoData.LiveStatus);

        var video = await _videoRepository.FindByExternalId(youtubeVideoId, userId, Access.Read, transaction);
        var isExistingVideo = video is not null;

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

        type ??= videoData.GetVideoType();

        Period? duration = null;
        if (videoData.Duration is { } durationInSeconds)
        {
            duration = Period.FromSeconds((long)Math.Truncate(durationInSeconds));
        }
        else if (type != VideoType.Livestream)
        {
            throw new InvalidOperationException("Video is missing duration");
        }

        if (video is null)
        {
            var externalUrl = videoData.WebpageUrl ?? throw new InvalidOperationException("Missing video url");

            var publishedAt = videoData.ReleaseTimestamp ?? videoData.Timestamp
                ?? throw new InvalidOperationException("Video is missing publish date");
            var publishedInstant = Instant.FromUnixTimeSeconds(publishedAt);

            video = await _videoService.Create(
                userId,
                channel,
                library.OwnerId,
                videoData.Title,
                videoData.Description ?? string.Empty,
                videoData.Categories ?? [],
                videoData.Tags ?? [],
                type,
                youtubeVideoId,
                externalUrl,
                publishedInstant,
                currentTime,
                availability,
                duration,
                videoData.ViewCount,
                videoData.LikeCount,
                transaction);
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

        var preferences = await _preferencesRepository.GetEffectiveForChannel(libraryId, channel.Id, userId, transaction, cancellationToken) ?? new();
        preferences.ApplyDefaults();

        var files = await _videoRepository.GetFilesAsync(video.Id, userId, transaction, cancellationToken);
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

        // todo: do not create files for unfinished livestreams, because they cannot be downloaded anyway
        if (videoData.LiveStatus is not (LiveStatus.None or LiveStatus.NotLive or LiveStatus.WasLive))
        {
            formatSelectors = [];
        }

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
                .Select(formatId =>
                {
                    var matching = data.Formats.Where(formatData => formatData.FormatId == formatId).ToArray();
                    if (matching is [var single])
                    {
                        return single;
                    }

                    if (matching is [])
                    {
                        throw new InvalidOperationException($"Could not find format by id {formatId}");
                    }

                    throw new InvalidOperationException($"Found multiple formats by id {formatId}");
                })
                .ToArray();

            var nonAudioFormats = formats.Where(formatData => formatData.Resolution is not "audio only").ToArray();
            if (nonAudioFormats is [var videoFormat])
            {
                videoFormats.Add(format, videoFormat);
            }
            else
            {
                throw new($"Unexpected formats {string.Join(", ", formats.Select(invalidFormat => invalidFormat.Format))}");
            }
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
                    Math.Round(file.Framerate) >= (decimal)Math.Round(videoFormat.FrameRate!.Value))
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

        var tracks = await _trackFileRepository.GetForVideo(video.Id, userId, Access.Read, transaction, cancellationToken);

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

            var chaptersFile = new FileInfo(chaptersFilePath);
            await using (var stream = chaptersFile.Create())
            {
                await _webVideoTextTracksService.Write(stream, chapterCues, cancellationToken);
            }

            await _trackFileService.CreateOrUpdateChapters(
                video,
                chaptersFile,
                tracks,
                HashAlgorithm.Default,
                userId,
                transaction,
                cancellationToken);
        }

        await CreateOrUpdateThumbnail(url, directory, cookieFilepath, userId, video, transaction, cancellationToken);

        if (isExistingVideo && !(videoData.LiveStatus is LiveStatus.WasLive && files.Any(file => file.DownloadedAt is not null)))
        {
            _logger.NotDownloadingExistingVideo();
            return video.Id;
        }

        if (preferences.DownloadVideos is null || preferences.DownloadVideos == DownloadVideos.None)
        {
            _logger.NotDownloadingDueToPreferences();
            return video.Id;
        }

        if (videoData.LiveStatus is LiveStatus.IsUpcoming or LiveStatus.IsLive or LiveStatus.PostLive)
        {
            _logger.NotDownloadingLiveVideo(video.Id);
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

        await _taskService.DownloadVideo(userId, libraryId, video.Id, source, transaction);
        return video.Id;
    }

    private async ValueTask CreateOrUpdateThumbnail(
        string url,
        DirectoryInfo directory,
        string? cookieFilepath,
        Guid userId,
        VideoEntity video,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        _logger.DownloadingThumbnail();

        var fileName = $"thumbnail_{video.Id:N}";
        await _ytdlpWrapper.DownloadThumbnail(url, directory.FullName, fileName, cookieFilepath, cancellationToken);

        var thumbnails = directory.EnumerateFiles($"{fileName}.*").ToArray();
        if (thumbnails is [])
        {
            throw new Exception("Could not find downloaded thumbnail");
        }

        if (thumbnails is not [var file])
        {
            throw new Exception("Multiple thumbnails");
        }

        var response = await _ffmpegService.AnalyzeFile(file.FullName, cancellationToken);
        if (response.Streams is not [var stream])
        {
            throw new InvalidOperationException("Unexpected image file format");
        }

        if (stream is not { Width: { } width, Height: { } height })
        {
            throw new InvalidOperationException("Could not extract image resolution");
        }

        var existing = await _imageFileRepository.FindVideoThumbnail(video.Id, userId, Access.Modify, transaction);
        if (existing is not null && existing.Width >= width)
        {
            _logger.ExistingThumbnail();
            return;
        }

        var hashAlgorithm = HashAlgorithm.Default;
        var hashData = await hashAlgorithm.ComputeHashAsync(file, cancellationToken);

        if (existing is null)
        {
            _logger.CreatingThumbnail();

            var imageFileId = await _imageFileRepository.AddAsync(
                new()
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    StoragePath = file.Name,
                    StorageSize = file.Length,
                    Type = ImageType.Thumbnail,
                    Width = width,
                    Height = height,
                    HashAlgorithm = hashAlgorithm,
                    Hash = hashData,
                },
                transaction);

            await _imageFileRepository.LinkToVideoAsync(imageFileId!.Value, video.Id, userId, transaction);
        }
        else
        {
            _logger.UpdatingExistingThumbnail(existing.Id);

            existing.ModifiedByUserId = userId;
            existing.StoragePath = file.Name;
            existing.StorageSize = file.Length;
            existing.Type = ImageType.Thumbnail;
            existing.Width = width;
            existing.Height = height;
            existing.HashAlgorithm = hashAlgorithm;
            existing.Hash = hashData;

            var count = await _imageFileRepository.UpdateAsync(existing, transaction);
            if (count is 0)
            {
                throw new InvalidOperationException("Failed to update existing thumbnail");
            }
        }

        var destinationFileName = Path.Combine(video.StoragePath, file.Name);
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

    public ValueTask ScanChannel(Guid libraryId,
        Guid channelId,
        bool allVideos,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        TaskSource source,
        CookiesService cookiesService,
        CancellationToken cancellationToken)
    {
        return ScanChannelCore(libraryId, channelId, allVideos, false, userId, taskRepository, taskRunId, tempDirectory, source, cookiesService, cancellationToken);
    }

    public async ValueTask ScanSubscriptions(
        Guid libraryId,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        TaskSource source,
        CookiesService cookiesService,
        CancellationToken cancellationToken)
    {
        List<ChannelEntity> channels;

        await using (var transaction = await _connection.OpenAndBeginTransaction(RepeatableRead, cancellationToken))
        {
            channels = await _channelRepository.GetSubscribedForLibrary(libraryId, userId, transaction);
            await transaction.CommitAsync(cancellationToken);
        }

        await taskRepository.InitializeTaskProgress(taskRunId, channels.Count);
        _logger.ScanningSubscribedChannels(channels.Count);

        var startTime = _clock.GetCurrentInstant();
        var totalCount = channels.Count;

        foreach (var (index, channel) in channels.Index())
        {
            await ScanChannelCore(libraryId, channel.Id, false, true, userId, taskRepository, taskRunId, tempDirectory, source, cookiesService, cancellationToken, false);

            var currentIndex = index + 1;
            var (rate, remaining) = _clock.GetRemainingEstimate(startTime, totalCount, currentIndex);
            await taskRepository.UpdateProgress(taskRunId, currentIndex, rate, remaining);
        }
    }

    private async ValueTask ScanChannelCore(
        Guid libraryId,
        Guid channelId,
        bool allVideos,
        bool breakOnExisting,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo directory,
        TaskSource source,
        CookiesService cookiesService,
        CancellationToken cancellationToken,
        bool reportProgress = true)
    {
        var cookiesFilepath = await cookiesService.RefreshCookieFile();

        ChannelEntity channel;
        PreferencesEntity preferences;

        await using (var transaction = await _connection.OpenAndBeginTransaction(RepeatableRead, cancellationToken))
        {
            channel = await _channelRepository.GetAsync(channelId, userId, transaction);
            preferences = await _preferencesRepository.GetEffectiveForChannel(libraryId, channelId, userId, transaction, cancellationToken) ?? new();

            await transaction.CommitAsync(cancellationToken);
        }

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

        var totalCount = playlists.SelectMany(pair => pair.Value.Entries ?? []).Count();
        if (reportProgress)
        {
            await taskRepository.InitializeTaskProgress(taskRunId, totalCount);
        }

        var startTime = _clock.GetCurrentInstant();
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

                await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
                var existing = await _videoRepository.FindByExternalUrl(video.Url, userId, Access.Read, transaction);
                if (existing is not null && breakOnExisting)
                {
                    _logger.ChannelScanExistingVideo(existing.ExternalUrl);

                    if (reportProgress)
                    {
                        var currentIndex = playlistData.Entries.Length + indexOffset;
                        var (rate, remaining) = _clock.GetRemainingEstimate(startTime, totalCount, currentIndex);
                        await taskRepository.UpdateProgress(taskRunId, currentIndex, rate, remaining);
                    }

                    await transaction.CommitAsync(cancellationToken);
                    break;
                }

                cookiesFilepath = await cookiesService.RefreshCookieFile();
                var videoResult = await _ytdlpWrapper.FetchVideoData(video.Url, cookiesFilepath, cancellationToken);
                if (!videoResult.Success)
                {
                    _logger.ChannelScanFailedVideo(video.Url, string.Join(Environment.NewLine, videoResult.ErrorOutput));
                    await transaction.CommitAsync(cancellationToken);
                    continue;
                }

                await IndexVideo(video.Url, channel, libraryId, userId, videoResult.Data, transaction, directory, source, cookiesService, cancellationToken, type);

                if (reportProgress)
                {
                    var currentIndex = index + 1 + indexOffset;
                    var (rate, remaining) = _clock.GetRemainingEstimate(startTime, totalCount, currentIndex);
                    await taskRepository.UpdateProgress(taskRunId, currentIndex, rate, remaining);
                }

                await transaction.CommitAsync(cancellationToken);
            }

            indexOffset += playlistData.Entries.Length;
        }
    }

    public async ValueTask Reindex(Guid libraryId, Guid userId, TaskSource source, CancellationToken cancellationToken)
    {
        // we only read data at the start of the transaction, so ReadCommitted is ok
        await using var transaction = await _connection.OpenAndBeginTransaction(ReadCommitted, cancellationToken);

        foreach (var (videoId, channelId, videoUrl) in await _videoRepository.GetForReindex(libraryId, transaction))
        {
            await _taskService.IndexVideo(userId, libraryId, videoUrl, channelId, videoId, source, transaction, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
