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
    private readonly IClock _clock;
    private readonly NpgsqlConnection _connection;

    private static string VideoFormat { get; } = string.Join(',', VideoFormats);

    public YoutubeService(ILogger<YoutubeService> logger,
        IOptionsMonitor<YtdlpOptions> optionsMonitor,
        ChannelRepository channelRepository,
        VideoRepository videoRepository,
        VideoFileRepository videoFileRepository,
        ImageFileRepository imageFileRepository,
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
        _imageFileRepository = imageFileRepository;
        _videoFileRepository = videoFileRepository;
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
                NoPart = true,
                EmbedChapters = true,
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

            // var result = await youtube.RunWithOptions(
            //     videoData.Data.ChannelUrl,
            //     new OptionSet
            //     {
            //         Output = "thumbnail:thumbnail.%(ext)s",
            //         Paths = $"{channel.StoragePath}",
            //         SkipDownload = true,
            //         LimitRate = 1024 * 1024 * 10,
            //         Cookies = cookieFilepath,
            //         WriteAllThumbnails = true,
            //         PlaylistItems = "0",
            //     },
            //     cancellationToken);
            //
            // var thumbnails = Directory.EnumerateFiles(channel.StoragePath);
            // foreach (var thumbnail in thumbnails)
            // {
            //     var type = Path.GetFileNameWithoutExtension(thumbnail) switch
            //     {
            //         var fileName when fileName.EndsWith("avatar_uncropped") => ImageType.Thumbnail,
            //         var fileName when fileName.EndsWith("banner_uncropped") => ImageType.Banner,
            //         _ => null,
            //     };
            //
            //     if (type is null)
            //     {
            //         File.Delete(thumbnail);
            //         continue;
            //     }
            //
            //     File.Move(
            //         thumbnail,
            //         Path.Combine(channel.StoragePath, $"{type.Name}{Path.GetExtension(thumbnail)}"));
            // }
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

        var files = await _videoRepository.GetFilesAsync(video.Id, userId, transaction);
        foreach (var format in VideoFormats)
        {
            var result = await youtube.RunVideoDataFetch(
                url,
                cancellationToken,
                true,
                false,
                new OptionSet
                {
                    Cookies = cookieFilepath,
                    Format = format,
                    NoPart = true,
                    EmbedChapters = true,
                });

            if (!result.Success || result.Data is not { ResultType: MetadataType.Video } data)
            {
                throw new Exception("Unexpected result");
            }

            var formatIds = data.FormatID.Split('+');
            var formats = formatIds
                .Select(formatId => data.Formats.Single(formatData => formatData.FormatId == formatId))
                .ToArray();

            var videoFormat = formats.Single(formatData => formatData.Resolution is not "audio only");
            var containerType = VideoContainerType.FromName(videoFormat.Extension);

            var file = files.SingleOrDefault(file =>
                file.Type == containerType &&
                file.Width == videoFormat.Width &&
                Math.Round(file.Framerate) == (decimal)Math.Round(videoFormat.FrameRate!.Value));

            if (file is null)
            {
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

            _logger.LogDebug("Video file {VideoFile}", file);
        }

        var thumbnail = videoData
            .Data
            .Thumbnails
            .Where(data => new UriBuilder(data.Url).Query.StartsWith("?sqp") && data.Width.HasValue)
            .OrderByDescending(data => data.Width)
            .First();

        _ = await youtube.RunWithOptions(
            thumbnail.Url,
            new OptionSet
            {
                Output = "thumbnail.%(ext)s",
                Paths = $"{video.StoragePath}",
                Cookies = cookieFilepath,
            },
            cancellationToken);

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

            var imageFile = await _imageFileRepository.GetAsync(imageFileId!.Value, userId, transaction);
            await _imageFileRepository.LinkToVideoAsync(imageFile.Id, video.Id, userId, transaction);
        }
        else if (thumbnails is not [])
        {
            throw new Exception("Multiple thumbnails");
        }

        await transaction.CommitWithRetries(_logger, cancellationToken);
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

        var files = await _videoRepository.GetFilesAsync(videoId, userId, transaction);

        var tasks = VideoFormats.Select(async format =>
        {
            var result = await youtube.RunVideoDataFetch(
                video.ExternalUrl,
                cancellationToken,
                true,
                false,
                new OptionSet
                {
                    Cookies = cookieFilepath,
                    Format = format,
                    NoPart = true,
                    EmbedChapters = true,
                });

            return (result, format);
        });

        var results = await Task.WhenAll(tasks);
        if (results.Any(tuple => !tuple.result.Success))
        {
            var failedResults = results.Where(tuple => !tuple.result.Success).Select(tuple => tuple.result.ErrorOutput);
            throw new Exception(string.Join(Environment.NewLine, failedResults.SelectMany(lines => lines)));
        }

        if (results.Any(tuple => tuple.result.Data.ResultType is not MetadataType.Video))
        {
            throw new InvalidOperationException("Unexpected metadata type when downloading video");
        }

        var attributes = File.GetAttributes(video.StoragePath);
        var targetDirectory = (attributes & FileAttributes.Directory) is FileAttributes.Directory
            ? video.StoragePath
            : Path.GetDirectoryName(video.StoragePath)!;

        Directory.CreateDirectory(targetDirectory);

        foreach (var (data, selectedFormat) in results
                     .Where(tuple => tuple.result.Success && tuple.result.Data.ResultType is MetadataType.Video)
                     .Select(tuple => (tuple.result.Data, tuple.format)))
        {
            var formatIds = data.FormatID.Split('+');
            var formats = formatIds
                .Select(formatId => data.Formats.Single(format => format.FormatId == formatId))
                .ToArray();

            var videoFormat = formats.Single(format => format.Resolution is not "audio only");
            var containerType = VideoContainerType.FromName(videoFormat.Extension);
            var videoFile = files.Single(file => file.Type == containerType && file.Height == videoFormat.Height!.Value);

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
                // await taskRepository.InitializeTaskProgress(taskRunId, size.Value);
            }

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
                    LimitRate = 1024 * 1024 * 10,
                    Cookies = cookieFilepath,
                    WriteSubs = true,
                    NoWriteAutoSubs = true,
                    SubFormat = "vtt",
                    SubLangs = "all,-live_chat",
                    NoPart = true,
                    EmbedChapters = true,
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

                if (size.HasValue)
                {
                    // await taskRepository.UpdateProgress(taskRunId, fileSize);
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
