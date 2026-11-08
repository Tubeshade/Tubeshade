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
using Tubeshade.Data.Media.Videos;
using Tubeshade.Data.Preferences;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration;
using Tubeshade.Server.Pages.Videos;
using Tubeshade.Server.Services.Ffmpeg;
using Tubeshade.Server.Services.Migrations;
using YoutubeDLSharp.Metadata;
using Ytdlp;
using Ytdlp.Processes;
using static System.Data.IsolationLevel;

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
        VideoEntity video;
        List<VideoFileEntity> files;
        PreferencesEntity preferences;

        await using (var transaction = await _connection.OpenAndBeginTransaction(RepeatableRead, cancellationToken))
        {
            video = await _videoRepository.GetAsync(videoId, userId, transaction);
            files = await _videoRepository.GetFilesAsync(videoId, userId, transaction, cancellationToken);
            preferences = await _preferencesRepository.GetEffectiveForVideo(libraryId, videoId, userId, transaction, cancellationToken) ?? new();
            preferences.ApplyDefaults();

            await transaction.CommitAsync(cancellationToken);
        }

        if (preferences.DownloadMethod?.Name is null or DownloadMethod.Names.Default)
        {
            await DownloadDefault(userId, taskRepository, taskRunId, tempDirectory, preferences, video, files, cookiesService, cancellationToken);
        }
        else if (preferences.DownloadMethod == DownloadMethod.Streaming)
        {
            await DownloadStreaming(userId, taskRepository, taskRunId, tempDirectory, preferences, video, files, provider, cookiesService, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"Unexpected download method '{preferences.DownloadMethod}'");
        }

        await using (var transaction = await _connection.OpenAndBeginTransaction(cancellationToken))
        {
            var tracks = await _trackFileRepository.GetForVideo(video.Id, userId, Access.Read, transaction, cancellationToken);

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
    }

    private async ValueTask DownloadDefault(
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        PreferencesEntity preferences,
        VideoEntity video,
        List<VideoFileEntity> files,
        CookiesService cookiesService,
        CancellationToken cancellationToken)
    {
        var formatSelectors = preferences.Formats is { Length: > 0 } preferredFormats
            ? preferredFormats
            : YoutubeIndexingService.DefaultVideoFormats;

        var cookieFilepath = await cookiesService.RefreshCookieFile();
        var dataFile = await FetchDataFile(video, preferences, tempDirectory, cookieFilepath, cancellationToken);
        var selectedFormats = await _ytdlpWrapper.SelectFormats(dataFile.Path, formatSelectors, cancellationToken);

        var totalSize = selectedFormats
            .SelectMany(formats => formats)
            .Sum(format => (decimal?)(format.FileSize ?? format.ApproximateFileSize));

        if (totalSize.HasValue)
        {
            await taskRepository.InitializeTaskProgress(taskRunId, totalSize.Value);
        }

        var targetDirectoryPath = video.GetDirectoryPath();
        var targetDirectory = Directory.CreateDirectory(targetDirectoryPath);

        var sizeOffset = 0m;

        foreach (var formats in selectedFormats)
        {
            foreach (var formatData in formats)
            {
                _logger.LogDebug("Selected format {FormatData}", formatData);
            }

            var videoFormat = formats.Single(format => !format.Resolution!.Contains("audio only", StringComparison.OrdinalIgnoreCase));
            var containerType = VideoContainerType.FromName(videoFormat.Extension);
            var videoFile = GetFileForFormat(files, videoFormat, containerType);

            var size = formats.Sum(format => (decimal?)(format.FileSize ?? format.ApproximateFileSize));
            if (videoFile.DownloadedAt is not null)
            {
                _logger.ExistingVideoFile(videoFile.Id);
                sizeOffset += size ?? 0;
                continue;
            }

            var fileName = videoFile.StoragePath;
            var limitRate = GetLimitRate(size, video);

            cookieFilepath = await cookiesService.RefreshCookieFile();
            await RefreshExpiringDataFile(dataFile, formats, EstimateDownloadDuration(size, limitRate), video, preferences, cookieFilepath, cancellationToken);

            var downloadTask = _ytdlpWrapper.DownloadVideo(
                dataFile.Path,
                string.Join('+', formats.Select(format => format.FormatId)),
                videoFile.Type,
                tempDirectory.FullName,
                $"{Path.GetFileNameWithoutExtension(fileName)}.%(ext)s",
                cookieFilepath,
                limitRate,
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
            if (!downloadResult.Success)
            {
                throw new Exception(string.Join(Environment.NewLine, downloadResult.ErrorOutput));
            }

            await MoveFileToOutput(userId, tempDirectory, videoFile, video, targetDirectory, fileName, false, cancellationToken);

            sizeOffset += size ?? 0;
        }

        if (cookieFilepath is not null && File.Exists(cookieFilepath))
        {
            File.Delete(cookieFilepath);
        }

        File.Delete(dataFile.Path);

        foreach (var tempFile in tempDirectory.EnumerateFiles())
        {
            var targetFilePath = Path.Combine(targetDirectoryPath, tempFile.Name);
            _logger.MovingFile(tempFile.FullName, targetFilePath);
            tempFile.MoveTo(targetFilePath);
        }
    }

    private async ValueTask DownloadStreaming(
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        PreferencesEntity preferences,
        VideoEntity video,
        List<VideoFileEntity> files,
        IServiceProvider provider,
        CookiesService cookiesService,
        CancellationToken cancellationToken)
    {
        var formatSelectors = preferences.Formats is { Length: > 0 } preferredFormats
            ? preferredFormats
            : YoutubeIndexingService.DefaultVideoFormats;

        var cookieFilepath = await cookiesService.RefreshCookieFile();
        var dataFile = await FetchDataFile(video, preferences, tempDirectory, cookieFilepath, cancellationToken);
        var selectedFormats = await _ytdlpWrapper.SelectFormats(dataFile.Path, formatSelectors, cancellationToken);

        var totalSize = selectedFormats
            .SelectMany(formats => formats)
            .Sum(format => (decimal?)(format.FileSize ?? format.ApproximateFileSize));

        if (totalSize.HasValue)
        {
            await taskRepository.InitializeTaskProgress(taskRunId, totalSize.Value);
        }

        var targetDirectory = Directory.CreateDirectory(video.GetDirectoryPath());
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
                targetDirectory,
                dataFile,
                cookieFilepath,
                preferences,
                totalSize,
                sizeOffset,
                provider,
                cancellationToken);
        }

        if (cookieFilepath is not null && File.Exists(cookieFilepath))
        {
            File.Delete(cookieFilepath);
        }

        File.Delete(dataFile.Path);

        foreach (var tempFile in tempDirectory.EnumerateFiles())
        {
            var targetFilePath = Path.Combine(targetDirectory.FullName, tempFile.Name);
            _logger.MovingFile(tempFile.FullName, targetFilePath);
            tempFile.MoveTo(targetFilePath);
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
        DirectoryInfo targetDirectory,
        DataFile dataFile,
        string? cookieFilepath,
        PreferencesEntity preferences,
        decimal? totalSize,
        decimal sizeOffset,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        if (formats is [var combinedFormat])
        {
            var containerType = VideoContainerType.FromName(combinedFormat.Extension);
            var videoFile = GetFileForFormat(files, combinedFormat, containerType);

            _logger.DownloadingCombinedVideoFile(videoFile.Id, combinedFormat.FormatId, containerType.Name);
            return await DownloadCombinedVideoFormat(
                userId,
                taskRepository,
                taskRunId,
                tempDirectory,
                combinedFormat,
                videoFile,
                video,
                targetDirectory,
                dataFile,
                cookieFilepath,
                preferences,
                totalSize,
                sizeOffset,
                provider,
                cancellationToken);
        }

        if (formats.Length > 1 && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var audioFormats = formats.Where(format => format.Resolution is "audio only").ToArray();
            if (formats.Except(audioFormats).ToArray() is not [var videoFormat] || audioFormats is [])
            {
                throw new ArgumentOutOfRangeException(nameof(formats), formats, @"Could not identify video and audio formats");
            }

            var containerType = VideoContainerType.FromName(videoFormat.Extension);
            var videoFile = GetFileForFormat(files, videoFormat, containerType);

            _logger.DownloadingSplitVideoFile(videoFile.Id, videoFormat.FormatId, string.Join('+', audioFormats.Select(format => format.FormatId)), containerType.Name);
            return await DownloadSplitVideoFormat(
                userId,
                taskRepository,
                taskRunId,
                tempDirectory,
                videoFormat,
                audioFormats,
                videoFile,
                video,
                targetDirectory,
                dataFile,
                cookieFilepath,
                preferences,
                totalSize,
                sizeOffset,
                provider,
                cancellationToken);
        }

        throw new ArgumentOutOfRangeException(nameof(formats), formats, $@"Unsupported format count {formats.Length}");
    }

    private async ValueTask<decimal> DownloadCombinedVideoFormat(
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        FormatData format,
        VideoFileEntity videoFile,
        VideoEntity video,
        DirectoryInfo targetDirectory,
        DataFile dataFile,
        string? cookieFilepath,
        PreferencesEntity preferences,
        decimal? totalSize,
        decimal sizeOffset,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var size = (decimal)(format.FileSize ?? format.ApproximateFileSize ?? 0);
        if (videoFile.DownloadedAt is not null)
        {
            _logger.ExistingVideoFile(videoFile.Id);
            return size;
        }

        var fileName = videoFile.StoragePath;
        var limitRate = GetLimitRate(size, video);
        await RefreshExpiringDataFile(dataFile, [format], EstimateDownloadDuration(size, limitRate), video, preferences, cookieFilepath, cancellationToken);

        await using (var scope = provider.CreateAsyncScope())
        {
            var fileRepository = scope.ServiceProvider.GetRequiredService<VideoFileRepository>();

            var path = Path.Combine(tempDirectory.FullName, fileName);
            await fileRepository.CreateTemporaryFile(videoFile.Id, taskRunId, path, cancellationToken);
        }

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var downloadTask = _ytdlpWrapper.DownloadVideo(
            dataFile.Path,
            format.FormatId,
            videoFile.Type,
            tempDirectory.FullName,
            $"{fileNameWithoutExtension}.%(ext)s",
            cookieFilepath,
            limitRate,
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
        if (!downloadResult.Success)
        {
            _logger.LogWarning("Failed to download video {VideoUrl}", video.ExternalUrl);
            throw new Exception(string.Join(Environment.NewLine, downloadResult.ErrorOutput));
        }

        await MoveFileToOutput(userId, tempDirectory, videoFile, video, targetDirectory, fileName, true, cancellationToken);

        return size;
    }

    [SupportedOSPlatform("linux")]
    private async ValueTask<decimal> DownloadSplitVideoFormat(
        Guid userId,
        TaskRepository taskRepository,
        Guid taskRunId,
        DirectoryInfo tempDirectory,
        FormatData videoFormat,
        FormatData[] audioFormats,
        VideoFileEntity videoFile,
        VideoEntity video,
        DirectoryInfo targetDirectory,
        DataFile dataFile,
        string? cookieFilepath,
        PreferencesEntity preferences,
        decimal? totalSize,
        decimal sizeOffset,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var size = audioFormats
            .Append(videoFormat)
            .Sum(format => (decimal)(format.FileSize ?? format.ApproximateFileSize ?? 0));

        if (videoFile.DownloadedAt is not null)
        {
            _logger.ExistingVideoFile(videoFile.Id);
            return size;
        }

        var audioFilePaths = audioFormats
            .Select(format => Path.Combine(tempDirectory.FullName, $"{videoFile.Id}_{format.FormatId}"))
            .ToArray();

        var videoFileName = $"{videoFile.Id}_{videoFormat.FormatId}";
        var videoFilePath = Path.Combine(tempDirectory.FullName, videoFileName);

        var fileName = videoFile.StoragePath;
        var outputFilePath = Path.Combine(tempDirectory.FullName, fileName);

        var filePaths = audioFilePaths.Prepend(videoFilePath).ToArray();

        const libc.Mode mask = libc.Mode.S_IWUSR | libc.Mode.S_IRUSR | libc.Mode.S_IRGRP | libc.Mode.S_IROTH;

        foreach (var path in filePaths)
        {
            if (libc.mkfifo(path, mask) is not 0)
            {
                throw new Exception($"Failed to create FIFO named pipe {path}");
            }
        }

        var audioSizes = audioFormats.Select(format => format.FileSize ?? format.ApproximateFileSize).ToArray();
        var videoSize = videoFormat.FileSize ?? videoFormat.ApproximateFileSize;
        var audioLimitRates = audioSizes.Select(audioSize => GetLimitRate(audioSize, video)).ToArray();
        var videoLimitRate = GetLimitRate(videoSize, video);

        foreach (var audioLimitRate in audioLimitRates)
        {
            _logger.SplitFormatLimitRates(videoFile.Id, audioLimitRate, videoLimitRate);
        }

        var downloadDurations = audioSizes
            .Zip(audioLimitRates, (audioSize, audioLimitRate) => EstimateDownloadDuration(audioSize, audioLimitRate))
            .Append(EstimateDownloadDuration(videoSize, videoLimitRate))
            .ToArray();

        var downloadDuration = downloadDurations.Contains(null) ? null : downloadDurations.Max();

        await RefreshExpiringDataFile(dataFile, [videoFormat, .. audioFormats], downloadDuration, video, preferences, cookieFilepath, cancellationToken);

        await using var processes = new AsyncDisposableList<CancelableProcess>();
        processes.Add(CreateProcess(videoFormat, videoFilePath, videoLimitRate));
        foreach (var (audioFormat, audioFilePath, audioLimitRate) in audioFormats.Zip(audioFilePaths, audioLimitRates))
        {
            processes.Add(CreateProcess(audioFormat, audioFilePath, audioLimitRate));
        }

        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var stoppingToken = stopping.Token;
        var failure = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var downloadTasks = new List<Task>(processes.Count);
        downloadTasks.AddRange(processes.Select(process => StopOnFailure(RunYtdlp(process, stoppingToken), stopping)));

        var downloadTask = Task.WhenAll(downloadTasks);
        var combineTask = StopOnFailure(_ffmpegService.CombineStreams(videoFilePath, audioFilePaths, outputFilePath, stoppingToken), stopping);

        // opening a FIFO blocks until ffmpeg opens it, which it does only after reading from the previous input
        var openTasks = filePaths
            .Select(path => Task.Run(() => File.Open(path, FileMode.Open, FileAccess.Write, FileShare.Read), CancellationToken.None))
            .ToArray();

        var copyTasks = new List<Task>();
        copyTasks.AddRange(processes.Zip(openTasks).Select(tuple => StopOnFailure(WaitForCombineOnBrokenPipe(CopyToFifo(tuple.First, tuple.Second, stoppingToken)), stopping)));
        var copyTask = Task.WhenAll(copyTasks);

        try
        {
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
            _logger.ClosedFifoStreams();

            await combineTask;
            _logger.FinishedCombiningSplitFile();
        }
        finally
        {
            await stopping.CancelAsync();
            await combineTask;

            // ffmpeg has exited without opening these FIFOs, so the threads opening them need a reader to be released
            foreach (var (path, openTask) in filePaths.Zip(openTasks).Where(pair => !pair.Second.IsCompleted))
            {
                await using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                }

                await StopOnFailure(openTask, stopping);
            }

            foreach (var openTask in openTasks.Where(task => task.IsCompletedSuccessfully))
            {
                await openTask.Result.DisposeAsync();
            }
        }

        if (failure.Task.IsCompleted)
        {
            await failure.Task;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await MoveFileToOutput(userId, tempDirectory, videoFile, video, targetDirectory, fileName, true, cancellationToken);

        foreach (var audioFilePath in audioFilePaths)
        {
            File.Delete(audioFilePath);
        }

        File.Delete(videoFilePath);

        return size;

        CancelableProcess CreateProcess(FormatData format, string filePath, long? limitRate)
        {
            // when writing to standard output yt-dlp stores HLS fragments in the working directory, and each stream needs its own
            var workingDirectory = Directory.CreateDirectory($"{filePath}_fragments");
            var options = _ytdlpWrapper.GetDownloadFormatArgs(dataFile.Path, format.FormatId, "-", cookieFilepath, limitRate);
            return new CancelableProcess(_options.YtdlpPath, options.ToArguments(string.Empty), false, workingDirectory: workingDirectory.FullName);
        }

        async Task StopOnFailure(Task task, CancellationTokenSource taskCancellationSource)
        {
            try
            {
                await task;
            }
            catch (Exception exception) when (!taskCancellationSource.IsCancellationRequested)
            {
                failure.TrySetException(exception);
                await taskCancellationSource.CancelAsync();
            }
            catch (Exception)
            {
                // Stopped because of another failure or because the download was canceled
            }
        }

        async Task WaitForCombineOnBrokenPipe(Task task)
        {
            try
            {
                await task;
            }
            catch (IOException) when (!stopping.IsCancellationRequested)
            {
                await Task.WhenAny(combineTask, Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None));
                throw;
            }
        }
    }

    private static async Task RunYtdlp(CancelableProcess process, CancellationToken cancellationToken)
    {
        if (await process.Run(cancellationToken) is not 0)
        {
            throw new Exception(YtdlpErrorOutput.GetErrorMessage(process.ErrorLines.ToArray()));
        }
    }

    private static async Task CopyToFifo(CancelableProcess process, Task<FileStream> openTask, CancellationToken cancellationToken)
    {
        await using var fifo = await openTask.WaitAsync(cancellationToken);
        await process.Output.CopyToAsync(fifo, cancellationToken);
        await fifo.FlushAsync(cancellationToken);
    }

    private async ValueTask<DataFile> FetchDataFile(
        VideoEntity video,
        PreferencesEntity preferences,
        DirectoryInfo tempDirectory,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        var path = DataFile.GetPath(tempDirectory);
        var data = await FetchVideoData(video, preferences, path, cookieFilepath, cancellationToken);

        return new DataFile(path, preferences.PlayerClient, data);
    }

    private async ValueTask RefreshExpiringDataFile(
        DataFile dataFile,
        FormatData[] formats,
        Duration? downloadDuration,
        VideoEntity video,
        PreferencesEntity preferences,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetCurrentInstant();
        var expiry = dataFile.Data.GetExpiry(formats.Select(format => format.FormatId));
        Instant? downloadEnd = downloadDuration is { } duration
            ? now + duration * 2 + Duration.FromMinutes(10)
            : null;

        var isReusable = (expiry, downloadEnd) switch
        {
            ({ } expiryTime, _) when expiryTime <= now => false,
            ({ } expiryTime, { } endTime) => endTime < expiryTime,
            _ => dataFile.FetchedAt is { } fetchedAt && now - fetchedAt < Duration.FromMinutes(10),
        };

        if (isReusable)
        {
            return;
        }

        _logger.RefreshingExpiringVideoData(video.Id, expiry, downloadEnd);
        var data = await FetchVideoData(video, preferences, dataFile.Path, cookieFilepath, cancellationToken);
        dataFile.Update(preferences.PlayerClient, data);
    }

    private async ValueTask<VideoData> FetchVideoData(
        VideoEntity video,
        PreferencesEntity preferences,
        string infoJsonFilePath,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        var result = await _ytdlpWrapper.FetchVideoData(
            video.ExternalUrl,
            cookieFilepath,
            preferences.PlayerClient,
            infoJsonFilePath,
            cancellationToken);

        if (result is { Success: true, Data: { Formats: [_, ..] } data })
        {
            return data;
        }

        throw new Exception(YtdlpErrorOutput.GetErrorMessage(result.ErrorOutput));
    }

    private long? GetLimitRate(decimal? size, VideoEntity video)
    {
        var limitRate = _options.LimitRate;
        if (size is not > 0 || _options.LimitMultiplier is not { } multiplier || video.Duration is not { } duration)
        {
            return limitRate;
        }

        var seconds = (decimal)duration.ToDuration().TotalSeconds;
        var byteRate = (long)Math.Round(size.Value * multiplier / seconds, 0);
        if (limitRate is null || limitRate.Value > byteRate)
        {
            limitRate = byteRate;
        }

        return limitRate;
    }

    private static VideoFileEntity GetFileForFormat(List<VideoFileEntity> files, FormatData format, VideoContainerType containerType)
    {
        var frameRate = format.GetFrameRate();

        return files.Single(file =>
            file.Type == containerType &&
            file.Width == format.Width!.Value &&
            Math.Abs(Math.Round(file.Framerate) - frameRate) <= 1);
    }

    private static Duration? EstimateDownloadDuration(decimal? size, long? limitRate) => size is > 0 && limitRate is > 0
        ? Duration.FromSeconds((double)(size.Value / limitRate.Value))
        : null;

    private async ValueTask MoveFileToOutput(
        Guid userId,
        DirectoryInfo tempDirectory,
        VideoFileEntity videoFile,
        VideoEntity video,
        DirectoryInfo targetDirectory,
        string fileName,
        bool remux,
        CancellationToken cancellationToken)
    {
        _logger.DownloadedVideoFile(videoFile.Id);

        var downloadedPath = Path.Combine(tempDirectory.FullName, fileName);

        if (remux || (videoFile.Type == VideoContainerType.Mp4 && !await Mp4Layout.IsStreamable(downloadedPath, cancellationToken)))
        {
            var remuxedPath = Path.Combine(
                tempDirectory.FullName,
                $"{Path.GetFileNameWithoutExtension(fileName)}_remux{Path.GetExtension(fileName)}");

            _logger.MovingFileFfmpeg(downloadedPath, remuxedPath);
            await _ffmpegService.Copy(downloadedPath, remuxedPath, cancellationToken);
            File.Move(remuxedPath, downloadedPath, true);
        }

        var file = new FileInfo(downloadedPath);
        var hashAlgorithm = HashAlgorithm.Default;
        var hash = await hashAlgorithm.ComputeHashAsync(file, cancellationToken);
        var currentTime = _clock.GetCurrentInstant();

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

        var latestFile = await _videoFileRepository.GetAsync(videoFile.Id, userId, transaction);
        if (latestFile.ModifiedAt != videoFile.ModifiedAt)
        {
            throw new InvalidOperationException("Video file was updated during download");
        }

        var latestVideo = await _videoRepository.GetAsync(video.Id, userId, transaction);
        if (latestVideo.ModifiedAt != video.ModifiedAt)
        {
            throw new InvalidOperationException("Video was updated during download");
        }

        videoFile.ModifiedAt = currentTime;
        videoFile.ModifiedByUserId = userId;
        videoFile.DownloadedAt = currentTime;
        videoFile.DownloadedByUserId = userId;
        videoFile.HashAlgorithm = hashAlgorithm;
        videoFile.Hash = hash;
        videoFile.StorageSize = file.Length;

        if (await _videoFileRepository.UpdateAsync(videoFile, transaction) < 1)
        {
            throw new InvalidOperationException($"User {userId} failed to update video file {videoFile.Id}");
        }

        if (video is not { IgnoredAt: null, IgnoredByUserId: null })
        {
            video.ModifiedAt = currentTime;
            video.ModifiedByUserId = userId;
            video.IgnoredAt = null;
            video.IgnoredByUserId = null;

            if (await _videoRepository.UpdateAsync(video, transaction) < 1)
            {
                throw new InvalidOperationException($"User {userId} failed to remove ignored status from video {video.Id}");
            }
        }

        var targetFilePath = Path.Combine(targetDirectory.FullName, fileName);
        var moved = false;

        try
        {
            _logger.MovingFile(file.FullName, targetFilePath);
            file.MoveTo(targetFilePath);
            moved = true;

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            if (moved && File.Exists(targetFilePath))
            {
                File.Delete(targetFilePath);
            }

            throw;
        }
    }
}
