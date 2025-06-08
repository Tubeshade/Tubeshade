using System;
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
using NodaTime.Text;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;
using static YoutubeDLSharp.Metadata.Availability;

namespace Tubeshade.Server.Services;

public sealed class YoutubeService
{
    private const string VideoFormat = "wv+(ba[format_note*=original]/ba)/worst";

    private readonly ILogger<YoutubeService> _logger;
    private readonly YtdlpOptions _options;
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly VideoRepository _videoRepository;
    private readonly IClock _clock;
    private readonly NpgsqlConnection _connection;

    public YoutubeService(
        ILogger<YoutubeService> logger,
        IOptionsMonitor<YtdlpOptions> optionsMonitor,
        ChannelRepository channelRepository,
        VideoRepository videoRepository,
        IClock clock,
        LibraryRepository libraryRepository,
        NpgsqlConnection connection)
    {
        _logger = logger;
        _options = optionsMonitor.CurrentValue;
        _channelRepository = channelRepository;
        _videoRepository = videoRepository;
        _clock = clock;
        _libraryRepository = libraryRepository;
        _connection = connection;
    }

    public async ValueTask IndexVideo(
        string url,
        Guid libraryId,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var cookieFilepath = await CreateCookieFile(libraryId, tempDirectory, transaction, cancellationToken);

        var youtube = new YoutubeDL
        {
            YoutubeDLPath = _options.YtdlpPath,
            FFmpegPath = _options.FfmpefgPath,
        };

        _logger.LogInformation("Getting video metadata for {VideoUrl}", url);
        var videoData = await youtube.RunVideoDataFetch(
            url,
            cancellationToken,
            true,
            false,
            new OptionSet
            {
                Cookies = cookieFilepath,
                Format = VideoFormat,
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

        var youtubeChannelId = videoData.Data.ChannelID;
        var youtubeVideoId = videoData.Data.ID;

        var library = await _libraryRepository.GetAsync(libraryId, userId, transaction);

        var channels = await _channelRepository.GetAsync(userId, transaction);
        var channel = channels.SingleOrDefault(channel => channel.ExternalId == youtubeChannelId);

        if (channel is null)
        {
            var channelId = await _channelRepository.AddAsync(
                new ChannelEntity
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    OwnerId = userId,
                    Name = videoData.Data.Channel,
                    StoragePath = library.StoragePath,
                    ExternalId = youtubeChannelId,
                },
                transaction);

            channel = await _channelRepository.GetAsync(channelId!.Value, userId, transaction);

            channel.StoragePath = Path.Combine(library.StoragePath, $"channel_{channel.Id}");
            await _channelRepository.UpdateAsync(channel, transaction);

            await _connection.ExecuteAsync(
                "INSERT INTO media.library_channels (library_id, channel_id) VALUES (@LibraryId, @ChannelId);",
                new { LibraryId = libraryId, ChannelId = channel.Id },
                transaction);

            Directory.CreateDirectory(channel.StoragePath);
            // todo: download avatar/banner
        }

        var videos = await _videoRepository.GetAsync(userId, transaction);
        var video = videos.SingleOrDefault(video => video.ExternalId == youtubeVideoId);

        if (video is null)
        {
            var publishedAt = videoData.Data.ReleaseTimestamp ?? videoData.Data.Timestamp;
            var publishedInstant = Instant.FromDateTimeUtc(publishedAt!.Value);
            var duration = Period.FromSeconds((long)Math.Truncate(videoData.Data.Duration!.Value));
            var availability = videoData.Data.Availability switch
            {
                Public or Unlisted => ExternalAvailability.Public,
                Private or PremiumOnly or SubscriberOnly or NeedsAuth => ExternalAvailability.Private,
                _ => throw new ArgumentOutOfRangeException(nameof(videoData.Data.Availability),
                    videoData.Data.Availability, "Unexpected availability value"),
            };

            var videoId = await _videoRepository.AddAsync(
                new VideoEntity
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    OwnerId = userId,
                    Name = videoData.Data.Title,
                    Description = videoData.Data.Description ?? string.Empty,
                    Categories = videoData.Data.Categories ?? [],
                    Tags = videoData.Data.Tags ?? [],
                    ViewCount = videoData.Data.ViewCount,
                    LikeCount = videoData.Data.LikeCount,
                    ChannelId = channel.Id,
                    StoragePath = channel.StoragePath,
                    ExternalId = youtubeVideoId,
                    ExternalUrl = videoData.Data.WebpageUrl,
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

        await youtube.RunWithOptions(
            url,
            new OptionSet
            {
                Output = "thumbnail:thumbnail.%(ext)s",
                Paths = $"{video.StoragePath}",
                SkipDownload = true,
                LimitRate = 1024 * 1024,
                Cookies = cookieFilepath,
                WriteThumbnail = true,
            },
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
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
        var cookieFilepath = await CreateCookieFile(libraryId, tempDirectory, transaction, cancellationToken);

        var video = await _videoRepository.GetAsync(videoId, userId, transaction);

        var youtube = new YoutubeDL
        {
            YoutubeDLPath = _options.YtdlpPath,
            FFmpegPath = _options.FfmpefgPath,
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
                Format = VideoFormat,
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

        var formatIds = videoData.Data.FormatID.Split('+');
        var formats = formatIds
            .Select(formatId => videoData.Data.Formats.Single(format => format.FormatId == formatId))
            .ToArray();

        foreach (var formatData in formats)
        {
            _logger.LogDebug("Selected format {FormatData}", formatData);
        }

        var size = formats.Sum(format => (decimal?)(format.FileSize ?? format.ApproximateFileSize));

        _logger.LogInformation(
            "Selected format with size {VideoSize} MB",
            size.HasValue ? Math.Round(size.Value / 1024 / 1024, 2) : null);

        if (size.HasValue)
        {
            await taskRepository.InitializeTaskProgress(taskRunId, size.Value);
        }

        var attributes = File.GetAttributes(video.StoragePath);
        var targetDirectory = (attributes & FileAttributes.Directory) is FileAttributes.Directory
            ? video.StoragePath
            : Path.GetDirectoryName(video.StoragePath)!;

        Directory.CreateDirectory(targetDirectory);

        if (video.DownloadedAt is not null &&
            Directory.EnumerateFiles(targetDirectory, $"{video.Id}.*").Any(file => !file.EndsWith("webp")))
        {
            _logger.LogInformation("Video already exists {VideoUrl}", video.ExternalUrl);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        youtube.OutputFolder = tempDirectory.FullName;
        youtube.OutputFileTemplate = "video.%(ext)s";

        var outputProgress = new Progress<string>(s => _logger.LogDebug("yt-dlp output: {Output}", s));

        _logger.LogInformation("Downloading video {VideoUrl} to {Directory}", video.ExternalUrl,
            tempDirectory.FullName);
        var downloadTask = youtube.RunVideoDownload(
            video.ExternalUrl,
            format: VideoFormat,
            DownloadMergeFormat.Unspecified,
            VideoRecodeFormat.None,
            cancellationToken,
            null,
            outputProgress,
            new OptionSet
            {
                LimitRate = 1024 * 1024,
                Cookies = cookieFilepath,
                WriteSubs = true,
                NoWriteAutoSubs = true,
                SubFormat = "vtt",
                SubLangs = "all,-live_chat",
                CustomOptions =
                [
                    new Option<string>("-t") { Value = "mp4" },
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

            storagePath = tempDirectory.EnumerateFiles("video*.*").ToArray() switch
            {
                [var file] => file.FullName,
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

            if (size.HasValue)
            {
                await taskRepository.UpdateProgress(taskRunId, fileSize);
            }

            var remaining2 = pollingDelay - Stopwatch.GetElapsedTime(startTimestamp);
            if (remaining2 < TimeSpan.Zero)
            {
                continue;
            }

            await Task.Delay(remaining2, cancellationToken);
        }

        var downloadResult = await downloadTask;

        if (videoData.Data.Chapters is { Length: > 0 })
        {
            await using var chapterFile = File.CreateText(Path.Combine(tempDirectory.FullName, "chapters.vtt"));
            await chapterFile.WriteLineAsync("WEBVTT");
            var pattern = DurationPattern.CreateWithInvariantCulture("HH:mm:ss.fff");

            foreach (var (chapter, index) in videoData.Data.Chapters.Select((data, index) => (data, index)))
            {
                var startTime = Duration.FromSeconds(chapter.StartTime ?? 0);
                var endTime = Duration.FromSeconds(videoData.Data.Duration ?? 0);

                await chapterFile.WriteLineAsync(
                    $"""
                     
                     {index + 1}
                     {pattern.Format(startTime)} --> {pattern.Format(endTime)}
                     {chapter.Title}
                     """);
            }
        }

        if (downloadResult.Success)
        {
            _logger.LogInformation("Downloaded video {VideoUrl}", video.ExternalUrl);

            if (cookieFilepath is not null && File.Exists(cookieFilepath))
            {
                File.Delete(cookieFilepath);
            }

            foreach (var file in tempDirectory.EnumerateFiles())
            {
                var targetFilePath = Path.Combine(targetDirectory, file.Name);
                _logger.LogDebug("Moving file from {SourcePath} to {TargetPath}", file.FullName, targetFilePath);
                file.MoveTo(targetFilePath);
            }

            video.StoragePath = Path.Combine(targetDirectory, "video.mp4");
            video.DownloadedAt = _clock.GetCurrentInstant();
            await _videoRepository.UpdateAsync(video, transaction);
        }
        else
        {
            _logger.LogWarning("Failed to download video {VideoUrl}", video.ExternalUrl);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async ValueTask<string?> CreateCookieFile(
        Guid libraryId,
        DirectoryInfo directory,
        NpgsqlTransaction transaction,
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
             WHERE id = @{nameof(libraryId)} AND domain LIKE '%youtube.com%';
             """,
            new { libraryId },
            transaction));

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
