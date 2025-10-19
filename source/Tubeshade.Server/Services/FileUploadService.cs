using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Services.Ffmpeg;
using Tubeshade.Server.V1;
using Stream = Tubeshade.Server.Services.Ffmpeg.Stream;

namespace Tubeshade.Server.Services;

public sealed class FileUploadService
{
    private readonly ILogger<FileUploadService> _logger;
    private readonly NpgsqlConnection _connection;
    private readonly VideoRepository _videoRepository;
    private readonly VideoFileRepository _videoFileRepository;
    private readonly IClock _clock;
    private readonly FileSystemService _fileSystemService;
    private readonly FfmpegService _ffmpegService;

    public FileUploadService(
        ILogger<FileUploadService> logger,
        NpgsqlConnection connection,
        VideoRepository videoRepository,
        VideoFileRepository videoFileRepository,
        IClock clock,
        FileSystemService fileSystemService,
        FfmpegService ffmpegService)
    {
        _logger = logger;
        _connection = connection;
        _videoRepository = videoRepository;
        _videoFileRepository = videoFileRepository;
        _clock = clock;
        _fileSystemService = fileSystemService;
        _ffmpegService = ffmpegService;
    }

    public async ValueTask<Guid> UploadVideoFile(
        Guid userId,
        Guid videoId,
        System.IO.Stream stream,
        string? contentType,
        CancellationToken cancellationToken)
    {
        var boundary = MediaTypeHeaderValue.Parse(contentType ?? string.Empty).GetBoundary();
        var reader = new MultipartReader(boundary, stream);
        var section = await reader.ReadNextSectionAsync(cancellationToken);
        if (section is null)
        {
            throw new ArgumentOutOfRangeException(nameof(stream), "Request contains 0 sections");
        }

        using var tempDirectory = _fileSystemService.CreateTemporaryDirectory("upload", Guid.NewGuid());

        var filePath = Path.Combine(tempDirectory.Directory.FullName, "video");
        await using (var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await section.Body.CopyToAsync(fileStream, cancellationToken);
        }

        var response = await _ffmpegService.AnalyzeFile(filePath, cancellationToken);
        if (response.Format is { FormatName: "matroska,webm" })
        {
            _logger.LogInformation("Detected MKV container format, re-muxing to WebM");
            filePath = await _ffmpegService.RemuxFile(filePath, VideoContainerType.WebM, response, cancellationToken);
            response = await _ffmpegService.AnalyzeFile(filePath, cancellationToken);
        }

        if (response.Streams is not [var first, var second])
        {
            throw new("Expected video to have 2 streams");
        }

        var (video, _) = (first, second) switch
        {
            _ when first.CodecType is "video" && second.CodecType is "audio" => (first, second),
            _ when first.CodecType is "audio" && second.CodecType is "video" => (second, first),
            _ => throw new("Expected video to have a single video and single audio stream")
        };

        var type = response.Format switch
        {
            { FormatName: "mov,mp4,m4a,3gp,3g2,mj2", Tags.MajorBrand: "isom" } => VideoContainerType.Mp4,
            { FormatName: "matroska,webm" } => VideoContainerType.WebM,
            _ => throw new("Unexpected video format")
        };

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var videoEntity = await _videoRepository.GetAsync(videoId, userId, transaction);
        var videoDirectory = videoEntity.GetDirectoryPath();
        var videoFilePath = Path.Combine(videoDirectory, $"video_{video.Height!.Value}.{type.Name}");

        var framerate = type.Name switch
        {
            VideoContainerType.Names.Mp4 => Math.Round(video.NbFrames!.Value / video.Duration!.Value, 0),
            VideoContainerType.Names.WebM => GetWebmDuration(video),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unexpected container type")
        };

        var videoFile = new VideoFileEntity
        {
            CreatedByUserId = userId,
            ModifiedByUserId = userId,
            OwnerId = userId,
            VideoId = videoId,
            StoragePath = videoFilePath,
            Type = type,
            Width = video.Width!.Value,
            Height = video.Height!.Value,
            Framerate = framerate,
            DownloadedAt = _clock.GetCurrentInstant(),
            DownloadedByUserId = userId,
        };

        var fileId = await _videoFileRepository.AddAsync(videoFile, transaction);
        File.Move(filePath, videoFilePath);

        await transaction.CommitAsync(cancellationToken);

        return fileId!.Value;
    }

    private static decimal GetWebmDuration(Stream stream)
    {
        if (stream.AvgFrameRate.Split('/') is not [var first, var second])
        {
            throw new ArgumentException("Format", nameof(stream));
        }

        var rate = decimal.Parse(first) / decimal.Parse(second);
        return Math.Round(rate, 0);
    }
}
