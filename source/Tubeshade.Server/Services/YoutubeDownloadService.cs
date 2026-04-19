using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
using Tubeshade.Server.Services.Migrations;
using YoutubeDLSharp.Metadata;
using Ytdlp;
using Ytdlp.Processes;

namespace Tubeshade.Server.Services;

public sealed class YoutubeDownloadService
{
    private readonly ILogger<YoutubeDownloadService> _logger;
    private readonly YtdlpOptions _options;
    private readonly VideoRepository _videoRepository;
    private readonly VideoFileRepository _videoFileRepository;
    private readonly PreferencesRepository _preferencesRepository;
    private readonly IClock _clock;
    private readonly NpgsqlConnection _connection;
    private readonly IYtdlpWrapper _ytdlpWrapper;
    private readonly FfmpegService _ffmpegService;
    private readonly TrackFileRepository _trackFileRepository;
    private readonly TrackFileService _trackFileService;

    public YoutubeDownloadService(
        ILogger<YoutubeDownloadService> logger,
        IOptionsMonitor<YtdlpOptions> optionsMonitor,
        VideoRepository videoRepository,
        VideoFileRepository videoFileRepository,
        IClock clock,
        NpgsqlConnection connection,
        PreferencesRepository preferencesRepository,
        IYtdlpWrapper ytdlpWrapper,
        FfmpegService ffmpegService,
        TrackFileRepository trackFileRepository,
        TrackFileService trackFileService)
    {
        _logger = logger;
        _options = optionsMonitor.CurrentValue;
        _videoRepository = videoRepository;
        _clock = clock;
        _connection = connection;
        _preferencesRepository = preferencesRepository;
        _ytdlpWrapper = ytdlpWrapper;
        _ffmpegService = ffmpegService;
        _trackFileRepository = trackFileRepository;
        _trackFileService = trackFileService;
        _videoFileRepository = videoFileRepository;
    }

    public async ValueTask DownloadVideo(
        Guid libraryId,
        Guid videoId,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        IServiceProvider provider,
        CookiesService cookiesService,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

        var video = await _videoRepository.GetAsync(videoId, userId, transaction);
        var tracks = await _trackFileRepository.GetForVideo(video.Id, userId, Access.Read, transaction, cancellationToken);
        var preferences = await _preferencesRepository.GetEffectiveForVideo(libraryId, videoId, userId, transaction, cancellationToken) ?? new();
        preferences.ApplyDefaults();

        if (preferences.DownloadMethod?.Name is null or DownloadMethod.Names.Default)
        {
            await DownloadDefault(videoId, userId, taskRepository, taskRunId, tempDirectory, preferences, video, transaction, cookiesService, cancellationToken);
        }
        else if (preferences.DownloadMethod == DownloadMethod.Streaming)
        {
            await DownloadStreaming(videoId, userId, taskRepository, taskRunId, tempDirectory, preferences, video, transaction, provider, cookiesService, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"Unexpected download method '{preferences.DownloadMethod}'");
        }

        var videoDirectory = new DirectoryInfo(video.GetDirectoryPath());
        await _trackFileService.CreateOrUpdateSubtitles(
            video,
            videoDirectory,
            tracks,
            HashAlgorithm.Default,
            userId,
            transaction,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private async ValueTask DownloadDefault(
        Guid videoId,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        PreferencesEntity preferences,
        VideoEntity video,
        NpgsqlTransaction transaction,
        CookiesService cookiesService,
        CancellationToken cancellationToken)
    {
        var formatSelectors = preferences.Formats is { Length: > 0 } preferredFormats
            ? preferredFormats
            : YoutubeIndexingService.DefaultVideoFormats;

        var files = await _videoRepository.GetFilesAsync(videoId, userId, transaction, cancellationToken);

        var cookieFilepath = await cookiesService.RefreshCookieFile();
        var selectedFormats = await _ytdlpWrapper.SelectFormats(
            video.ExternalUrl,
            formatSelectors,
            cookieFilepath,
            preferences.PlayerClient,
            cancellationToken);

        var totalSize = selectedFormats
            .SelectMany(formats => formats)
            .Sum(format => (decimal?)(format.FileSize ?? format.ApproximateFileSize));

        if (totalSize.HasValue)
        {
            await taskRepository.InitializeTaskProgress(taskRunId, totalSize.Value);
        }

        var targetDirectory = video.GetDirectoryPath();
        Directory.CreateDirectory(targetDirectory);

        var sizeOffset = 0m;

        foreach (var formats in selectedFormats)
        {
            foreach (var formatData in formats)
            {
                _logger.LogDebug("Selected format {FormatData}", formatData);
            }

            var videoFormat = formats.Single(format => !format.Resolution!.Contains("audio only", StringComparison.OrdinalIgnoreCase));
            var containerType = VideoContainerType.FromName(videoFormat.Extension);
            var videoFile = files.Single(file =>
                file.Type == containerType &&
                file.Width == videoFormat.Width!.Value &&
                Math.Round(file.Framerate) == (decimal)Math.Round(videoFormat.FrameRate!.Value));

            var size = formats.Sum(format => (decimal?)(format.FileSize ?? format.ApproximateFileSize));
            if (videoFile.DownloadedAt is not null)
            {
                _logger.ExistingVideoFile(videoFile.Id);
                sizeOffset += size ?? 0;
                continue;
            }

            var fileName = videoFile.StoragePath;

            var limitRate = _options.LimitRate;
            if (size.HasValue && _options.LimitMultiplier is { } multiplier && video.Duration is { } duration)
            {
                var seconds = (decimal)duration.ToDuration().TotalSeconds;
                var bitrate = (long)Math.Round(size.Value * 8 * multiplier / seconds, 0);
                if (limitRate is null || limitRate.Value > bitrate)
                {
                    limitRate = bitrate;
                }
            }

            cookieFilepath = await cookiesService.RefreshCookieFile();
            var downloadTask = _ytdlpWrapper.DownloadVideo(
                video.ExternalUrl,
                string.Join('+', formats.Select(format => format.FormatId)),
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
                _logger.DownloadedVideoFile(videoFile.Id);

                var file = new FileInfo(Path.Combine(tempDirectory.FullName, fileName));
                var hashAlgorithm = HashAlgorithm.Default;
                var hash = await hashAlgorithm.ComputeHashAsync(file, cancellationToken);
                var currentTime = _clock.GetCurrentInstant();

                videoFile.ModifiedAt = currentTime;
                videoFile.ModifiedByUserId = userId;
                videoFile.DownloadedAt = currentTime;
                videoFile.DownloadedByUserId = userId;
                videoFile.HashAlgorithm = hashAlgorithm;
                videoFile.Hash = hash;
                videoFile.StorageSize = file.Length;

                await _videoFileRepository.UpdateAsync(videoFile, transaction);

                sizeOffset += size ?? 0;
            }
            else
            {
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
            _logger.MovingFile(tempFile.FullName, targetFilePath);
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
    }

    private async ValueTask DownloadStreaming(
        Guid videoId,
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        PreferencesEntity preferences,
        VideoEntity video,
        NpgsqlTransaction transaction,
        IServiceProvider provider,
        CookiesService cookiesService,
        CancellationToken cancellationToken)
    {
        var formatSelectors = preferences.Formats is { Length: > 0 } preferredFormats
            ? preferredFormats
            : YoutubeIndexingService.DefaultVideoFormats;

        var files = await _videoRepository.GetFilesAsync(videoId, userId, transaction, cancellationToken);

        var cookieFilepath = await cookiesService.RefreshCookieFile();
        var selectedFormats = await _ytdlpWrapper.SelectFormats(
            video.ExternalUrl,
            formatSelectors,
            cookieFilepath,
            preferences.PlayerClient,
            cancellationToken);

        var totalSize = selectedFormats
            .SelectMany(formats => formats)
            .Sum(format => (decimal?)(format.FileSize ?? format.ApproximateFileSize));

        if (totalSize.HasValue)
        {
            await taskRepository.InitializeTaskProgress(taskRunId, totalSize.Value);
        }

        var targetDirectory = video.GetDirectoryPath();
        Directory.CreateDirectory(targetDirectory);

        var sizeOffset = 0m;

        foreach (var formats in selectedFormats)
        {
            cookieFilepath = await cookiesService.RefreshCookieFile();
            sizeOffset += await DownloadVideoFile(
                userId,
                taskRepository,
                taskRunId,
                tempDirectory,
                formats,
                files,
                video,
                cookieFilepath,
                preferences,
                totalSize,
                sizeOffset,
                provider,
                transaction,
                cancellationToken);
        }

        if (cookieFilepath is not null && File.Exists(cookieFilepath))
        {
            File.Delete(cookieFilepath);
        }

        var videoExtensions = VideoContainerType.List.Select(type => $".{type.Name}").ToArray();
        foreach (var tempFile in tempDirectory.EnumerateFiles())
        {
            var targetFilePath = Path.Combine(targetDirectory, tempFile.Name);

            if (videoExtensions.Contains(tempFile.Extension, StringComparer.OrdinalIgnoreCase))
            {
                _logger.MovingFileFfmpeg(tempFile.FullName, targetFilePath);
                await _ffmpegService.Copy(tempFile.FullName, targetFilePath, cancellationToken);
            }
            else
            {
                _logger.MovingFile(tempFile.FullName, targetFilePath);
                tempFile.MoveTo(targetFilePath);
            }
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
    }

    private async ValueTask<decimal> DownloadVideoFile(
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        FormatData[] formats,
        List<VideoFileEntity> files,
        VideoEntity video,
        string? cookieFilepath,
        PreferencesEntity preferences,
        decimal? totalSize,
        decimal sizeOffset,
        IServiceProvider provider,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (formats is [var combinedFormat])
        {
            var containerType = VideoContainerType.FromName(combinedFormat.Extension);
            var videoFile = files.Single(file =>
                file.Type == containerType &&
                file.Width == combinedFormat.Width!.Value &&
                Math.Round(file.Framerate) == (decimal)Math.Round(combinedFormat.FrameRate!.Value));

            _logger.DownloadingCombinedVideoFile(videoFile.Id, combinedFormat.FormatId, containerType.Name);
            return await DownloadCombinedVideoFormat(
                userId,
                taskRepository,
                taskRunId,
                tempDirectory,
                combinedFormat,
                videoFile,
                video,
                cookieFilepath,
                preferences,
                totalSize,
                sizeOffset,
                provider,
                transaction,
                cancellationToken);
        }

        if (formats is [var first, var second] && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var (videoFormat, audioFormat) = (first, second) switch
            {
                { first.Resolution: "audio only" } => (second, first),
                { second.Resolution: "audio only" } => (first, second),
                _ => throw new ArgumentOutOfRangeException(nameof(formats), formats, "Could not identify video and audio formats")
            };

            var containerType = VideoContainerType.FromName(videoFormat.Extension);
            var videoFile = files.Single(file =>
                file.Type == containerType &&
                file.Width == videoFormat.Width!.Value &&
                Math.Round(file.Framerate) == (decimal)Math.Round(videoFormat.FrameRate!.Value));

            _logger.DownloadingSplitVideoFile(videoFile.Id, videoFormat.FormatId, audioFormat.FormatId, containerType.Name);
            return await DownloadSplitVideoFormat(
                userId,
                taskRepository,
                taskRunId,
                tempDirectory,
                videoFormat,
                audioFormat,
                videoFile,
                video,
                cookieFilepath,
                preferences,
                totalSize,
                sizeOffset,
                provider,
                transaction,
                cancellationToken);
        }

        throw new ArgumentOutOfRangeException(nameof(formats), formats, $"Unsupported format count {formats.Length}");
    }

    private async ValueTask<decimal> DownloadCombinedVideoFormat(
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        FormatData format,
        VideoFileEntity videoFile,
        VideoEntity video,
        string? cookieFilepath,
        PreferencesEntity preferences,
        decimal? totalSize,
        decimal sizeOffset,
        IServiceProvider provider,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var size = (decimal)(format.FileSize ?? format.ApproximateFileSize ?? 0);
        if (videoFile.DownloadedAt is not null)
        {
            _logger.ExistingVideoFile(videoFile.Id);
            return size;
        }

        var fileName = videoFile.StoragePath;

        var limitRate = _options.LimitRate;
        if (size is not 0 && _options.LimitMultiplier is { } multiplier && video.Duration is { } duration)
        {
            var seconds = (decimal)duration.ToDuration().TotalSeconds;
            var bitrate = (long)Math.Round(size * 8 * multiplier / seconds, 0);
            if (limitRate is null || limitRate.Value > bitrate)
            {
                limitRate = bitrate;
            }
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var fileRepository = scope.ServiceProvider.GetRequiredService<VideoFileRepository>();

            var path = Path.Combine(tempDirectory.FullName, fileName);
            await fileRepository.CreateTemporaryFile(videoFile.Id, taskRunId, path, cancellationToken);
        }

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var downloadTask = _ytdlpWrapper.DownloadVideo(
            video.ExternalUrl,
            format.FormatId,
            videoFile.Type,
            tempDirectory.FullName,
            $"{fileNameWithoutExtension}.%(ext)s",
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
                    .EnumerateFiles($"{fileNameWithoutExtension}*.*")
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

            if (totalSize is { } total && size is not 0)
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
            _logger.DownloadedVideoFile(videoFile.Id);

            var file = new FileInfo(Path.Combine(tempDirectory.FullName, fileName));
            var hashAlgorithm = HashAlgorithm.Default;
            var hash = await hashAlgorithm.ComputeHashAsync(file, cancellationToken);
            var currentTime = _clock.GetCurrentInstant();

            videoFile.ModifiedAt = currentTime;
            videoFile.ModifiedByUserId = userId;
            videoFile.DownloadedAt = currentTime;
            videoFile.DownloadedByUserId = userId;
            videoFile.HashAlgorithm = hashAlgorithm;
            videoFile.Hash = hash;
            videoFile.StorageSize = file.Length;

            var count = await _videoFileRepository.UpdateAsync(videoFile, transaction);
            if (count is 0)
            {
                throw new("Failed to update video file after download");
            }

            return size;
        }

        _logger.LogWarning("Failed to download video {VideoUrl}", video.ExternalUrl);
        throw new Exception(string.Join(Environment.NewLine, downloadResult.ErrorOutput));
    }

    [SupportedOSPlatform("linux")]
    private async ValueTask<decimal> DownloadSplitVideoFormat(
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        FormatData videoFormat,
        FormatData audioFormat,
        VideoFileEntity videoFile,
        VideoEntity video,
        string? cookieFilepath,
        PreferencesEntity preferences,
        decimal? totalSize,
        decimal sizeOffset,
        IServiceProvider provider,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var size = new[] { audioFormat, videoFormat }
            .Sum(format => (decimal)(format.FileSize ?? format.ApproximateFileSize ?? 0));

        if (videoFile.DownloadedAt is not null)
        {
            _logger.ExistingVideoFile(videoFile.Id);
            return size;
        }

        var audioFileName = $"{videoFile.Id}_{audioFormat.FormatId}";
        var audioFilePath = Path.Combine(tempDirectory.FullName, audioFileName);

        var videoFileName = $"{videoFile.Id}_{videoFormat.FormatId}";
        var videoFilePath = Path.Combine(tempDirectory.FullName, videoFileName);

        var fileName = videoFile.StoragePath;
        var outputFilePath = Path.Combine(tempDirectory.FullName, fileName);

        const libc.Mode mask = libc.Mode.S_IWUSR | libc.Mode.S_IRUSR | libc.Mode.S_IRGRP | libc.Mode.S_IROTH;
        if (libc.mkfifo(videoFilePath, mask) is not 0)
        {
            throw new Exception($"Failed to create FIFO named pipe {videoFilePath}");
        }

        if (libc.mkfifo(audioFilePath, mask) is not 0)
        {
            throw new Exception($"Failed to create FIFO named pipe {audioFilePath}");
        }

        var audioLimitRate = _options.LimitRate;
        var videoLimitRate = _options.LimitRate;
        if (_options.LimitMultiplier is { } multiplier && video.Duration is { } duration)
        {
            var seconds = (decimal)duration.ToDuration().TotalSeconds;

            if ((audioFormat.FileSize ?? audioFormat.ApproximateFileSize) is { } audioFormatSize)
            {
                var bitrate = (long)Math.Round(audioFormatSize * 8 * multiplier / seconds, 0);
                if (audioLimitRate is null || audioLimitRate.Value > bitrate)
                {
                    audioLimitRate = bitrate;
                }
            }

            if ((videoFormat.FileSize ?? videoFormat.ApproximateFileSize) is { } videoFormatSize)
            {
                var bitrate = (long)Math.Round(videoFormatSize * 8 * multiplier / seconds, 0);
                if (videoLimitRate is null || videoLimitRate.Value > bitrate)
                {
                    videoLimitRate = bitrate;
                }
            }
        }

        _logger.SplitFormatLimitRates(videoFile.Id, audioLimitRate, videoLimitRate);

        var audioOptions = _ytdlpWrapper.GetDownloadFormatArgs(
            audioFormat.FormatId,
            "-",
            cookieFilepath,
            audioLimitRate,
            preferences.PlayerClient);

        var videoOptions = _ytdlpWrapper.GetDownloadFormatArgs(
            videoFormat.FormatId,
            "-",
            cookieFilepath,
            videoLimitRate,
            preferences.PlayerClient);

        using var audioProcess = new CancelableProcess(_options.YtdlpPath, audioOptions.ToArguments(video.ExternalUrl), false);
        using var videoProcess = new CancelableProcess(_options.YtdlpPath, videoOptions.ToArguments(video.ExternalUrl), false);

        var audioTask = audioProcess.Run(cancellationToken);
        var videoTask = videoProcess.Run(cancellationToken);
        var downloadTask = Task.WhenAll(audioTask, videoTask);

        var combineTask = _ffmpegService.CombineStreams(videoFilePath, audioFilePath, outputFilePath, cancellationToken);

        await using (var videoFileStream = File.Open(videoFilePath, FileMode.Open, FileAccess.Write, FileShare.Read))
        {
            var videoFileTask = videoProcess.Output.CopyToAsync(videoFileStream, cancellationToken);

            await using (var audioFileStream = File.Open(audioFilePath, FileMode.Open, FileAccess.Write, FileShare.Read))
            {
                var audioFileTask = audioProcess.Output.CopyToAsync(audioFileStream, cancellationToken);
                var copyTask = Task.WhenAll(videoFileTask, audioFileTask);

                await using (var scope = provider.CreateAsyncScope())
                {
                    var fileRepository = scope.ServiceProvider.GetRequiredService<VideoFileRepository>();
                    await fileRepository.CreateTemporaryFile(videoFile.Id, taskRunId, outputFilePath, cancellationToken);
                }

                var timestamp = Stopwatch.GetTimestamp();
                var fileSize = 0L;
                var pollingDelay = TimeSpan.FromSeconds(1);

                while (!downloadTask.IsCompleted)
                {
                    var startTimestamp = Stopwatch.GetTimestamp();
                    if (!File.Exists(outputFilePath))
                    {
                        await Task.Delay(pollingDelay, cancellationToken);
                        continue;
                    }

                    var newTimestamp = Stopwatch.GetTimestamp();
                    var newFileSize = new FileInfo(outputFilePath).Length;

                    var elapsedSeconds = Stopwatch.GetElapsedTime(timestamp, newTimestamp).TotalSeconds;
                    var sizeDelta = newFileSize - fileSize;

                    timestamp = newTimestamp;
                    fileSize = newFileSize;

                    if (totalSize is { } total && size is not 0)
                    {
                        var rate = sizeDelta / (decimal)elapsedSeconds;
                        var remainingSize = total - (newFileSize + sizeOffset);
                        var remainingDuration = rate > 0
                            ? Period.FromNanoseconds((long)(remainingSize / rate * 1_000_000_000)).Normalize()
                            : null;

                        await taskRepository.UpdateProgress(taskRunId, fileSize + sizeOffset, rate, remainingDuration);
                    }

                    var remaining = pollingDelay - Stopwatch.GetElapsedTime(startTimestamp);
                    if (remaining < TimeSpan.Zero)
                    {
                        continue;
                    }

                    await Task.Delay(remaining, cancellationToken);
                }

                await downloadTask;
                _logger.CompletedDownloadTasks();

                await copyTask;
                _logger.CompletedCopyTasks();

                await videoFileStream.FlushAsync(cancellationToken);
                await audioFileStream.FlushAsync(cancellationToken);
                _logger.FlushedFifoStreams();
            }
        }

        _logger.ClosedFifoStreams();

        await combineTask;
        _logger.FinishedCombiningSplitFile();

        _logger.DownloadedVideoFile(videoFile.Id);

        var file = new FileInfo(outputFilePath);
        var hashAlgorithm = HashAlgorithm.Default;
        var hash = await hashAlgorithm.ComputeHashAsync(file, cancellationToken);
        var currentTime = _clock.GetCurrentInstant();

        videoFile.ModifiedAt = currentTime;
        videoFile.ModifiedByUserId = userId;
        videoFile.DownloadedAt = currentTime;
        videoFile.DownloadedByUserId = userId;
        videoFile.HashAlgorithm = hashAlgorithm;
        videoFile.Hash = hash;
        videoFile.StorageSize = file.Length;

        var count = await _videoFileRepository.UpdateAsync(videoFile, transaction);
        if (count is 0)
        {
            throw new("Failed to update video file after download");
        }

        File.Delete(audioFilePath);
        File.Delete(videoFilePath);

        return size;
    }
}
