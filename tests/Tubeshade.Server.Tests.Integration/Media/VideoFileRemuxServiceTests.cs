using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration;
using Tubeshade.Server.Services.Ffmpeg;
using Tubeshade.Server.Services.Migrations;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Media;

public sealed class VideoFileRemuxServiceTests(ServerFixture fixture) : ServerTests(fixture)
{
    private const string IndexAtEndFileName = "index_at_end.mp4";
    private const string StreamableFileName = "streamable.mp4";
    private const string UnhashedFileName = "unhashed.mp4";

    private DirectoryInfo _directory = null!;
    private Guid _userId;
    private Guid _libraryId;
    private Guid _videoId;
    private Guid _indexAtEndFileId;
    private Guid _streamableFileId;
    private Guid _unhashedFileId;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _directory = Directory.CreateTempSubdirectory("tubeshade-remux");
        var videoPath = Path.Combine(_directory.FullName, "source_video.mp4");
        var audioPath = Path.Combine(_directory.FullName, "source_audio.m4a");

        await RunFfmpeg("-f", "lavfi", "-i", "testsrc=duration=5:size=320x240:rate=25", "-c:v", "mpeg4", videoPath);
        await RunFfmpeg("-f", "lavfi", "-i", "sine=duration=5", "-c:a", "aac", audioPath);
        await RunFfmpeg("-i", videoPath, "-i", audioPath, "-c", "copy", Path.Combine(_directory.FullName, IndexAtEndFileName));
        await RunFfmpeg("-i", videoPath, "-i", audioPath, "-c", "copy", "-movflags", "+faststart", Path.Combine(_directory.FullName, StreamableFileName));
        await RunFfmpeg("-i", videoPath, "-i", audioPath, "-c", "copy", Path.Combine(_directory.FullName, UnhashedFileName));

        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

        await using var transaction = await connection.OpenAndBeginTransaction();

        _userId = await scope.ServiceProvider.GetRequiredService<UserRepository>().GetSystemUserId(transaction);
        _libraryId = await CreateLibrary(_userId, scope.ServiceProvider, transaction);

        var channelRepository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();
        var channelId = (await channelRepository.AddAsync(
            new()
            {
                CreatedByUserId = _userId,
                ModifiedByUserId = _userId,
                OwnerId = _userId,
                Name = Guid.NewGuid().ToString("N"),
                StoragePath = string.Empty,
                ExternalId = string.Empty,
                ExternalUrl = string.Empty,
                Availability = ExternalAvailability.Public,
            },
            transaction))!.Value;

        await channelRepository.AddToLibrary(_libraryId, channelId, transaction);

        _videoId = (await scope.ServiceProvider.GetRequiredService<VideoRepository>().AddAsync(
            new()
            {
                CreatedByUserId = _userId,
                ModifiedByUserId = _userId,
                OwnerId = _userId,
                Name = Guid.NewGuid().ToString("N"),
                Description = string.Empty,
                Categories = [],
                Tags = [],
                Type = VideoType.Video,
                ChannelId = channelId,
                StoragePath = _directory.FullName,
                ExternalId = string.Empty,
                ExternalUrl = "https://example.org",
                PublishedAt = SystemClock.Instance.GetCurrentInstant(),
                RefreshedAt = SystemClock.Instance.GetCurrentInstant(),
                Availability = ExternalAvailability.Public,
                Duration = Period.FromSeconds(5),
            },
            transaction))!.Value;

        var fileRepository = scope.ServiceProvider.GetRequiredService<VideoFileRepository>();
        _indexAtEndFileId = (await fileRepository.AddAsync(await CreateHashedFile(IndexAtEndFileName), transaction))!.Value;
        _streamableFileId = (await fileRepository.AddAsync(await CreateHashedFile(StreamableFileName), transaction))!.Value;
        _unhashedFileId = (await fileRepository.AddAsync(CreateFile(UnhashedFileName), transaction))!.Value;

        await transaction.CommitAsync();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _directory.Delete(true);

    [Test]
    public async Task RemuxVideoFiles()
    {
        await RunRemux();

        var files = await GetFiles();

        using (new AssertionScope())
        {
            files.Should().HaveCount(3);
            files.Should().ContainSingle(file => file.Id == _streamableFileId);
            _directory.EnumerateFiles("*_remux*").Should().BeEmpty();
        }

        await ShouldBeRemuxed(files, IndexAtEndFileName, _indexAtEndFileId);
        await ShouldBeRemuxed(files, UnhashedFileName, _unhashedFileId);

        await RunRemux();

        var filesAfterSecondRun = await GetFiles();
        filesAfterSecondRun.Select(file => file.Id).Should().BeEquivalentTo(files.Select(file => file.Id));
    }

    private async Task ShouldBeRemuxed(VideoFileEntity[] files, string fileName, Guid originalId)
    {
        var remuxedFile = files.Should().ContainSingle(file => file.StoragePath == fileName).Subject;
        var path = Path.Combine(_directory.FullName, fileName);
        var hash = await HashAlgorithm.Default.ComputeHashAsync(new FileInfo(path), CancellationToken.None);
        var streamable = await Mp4Layout.IsStreamable(path, CancellationToken.None);

        using var scope = new AssertionScope(fileName);
        remuxedFile.Id.Should().NotBe(originalId, "the URL should change so that cached parts of the old file are not used");
        remuxedFile.Hash.Should().Equal(hash);
        remuxedFile.HashAlgorithm.Should().Be(HashAlgorithm.Default);
        remuxedFile.StorageSize.Should().Be(new FileInfo(path).Length);
        remuxedFile.DownloadedAt.Should().NotBeNull();
        streamable.Should().BeTrue();
    }

    private async Task<VideoFileEntity> CreateHashedFile(string fileName)
    {
        var hashAlgorithm = HashAlgorithm.Default;
        var file = new FileInfo(Path.Combine(_directory.FullName, fileName));

        return CreateFile(fileName) with
        {
            Hash = await hashAlgorithm.ComputeHashAsync(file, CancellationToken.None),
            HashAlgorithm = hashAlgorithm,
            StorageSize = file.Length,
        };
    }

    private VideoFileEntity CreateFile(string fileName) => new()
    {
        CreatedByUserId = _userId,
        ModifiedByUserId = _userId,
        OwnerId = _userId,
        VideoId = _videoId,
        StoragePath = fileName,
        Type = VideoContainerType.Mp4,
        Width = 320,
        Height = 240,
        Framerate = 25,
        DownloadedByUserId = _userId,
        DownloadedAt = SystemClock.Instance.GetCurrentInstant(),
    };

    private async Task RunRemux()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var taskRepository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

        var taskRunId = Guid.NewGuid();
        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var taskId = await taskRepository.AddTask(TaskEntity.RemuxVideos(_libraryId, _userId), transaction);
            await taskRepository.AddTaskRun(taskRunId, taskId, TaskSource.User, transaction);
            await transaction.CommitAsync();
        }

        var options = new ServiceCollection()
            .Configure<YtdlpOptions>(options =>
            {
                options.YtdlpPath = "yt-dlp";
                options.FfmpegPath = "ffmpeg";
                options.FfprobePath = "ffprobe";
            })
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<YtdlpOptions>>();

        var service = new VideoFileRemuxService(
            NullLogger<VideoFileRemuxService>.Instance,
            connection,
            scope.ServiceProvider.GetRequiredService<VideoRepository>(),
            scope.ServiceProvider.GetRequiredService<VideoFileRepository>(),
            new FfmpegService(NullLogger<FfmpegService>.Instance, options),
            SystemClock.Instance);

        await service.RemuxVideoFiles(_libraryId, _userId, taskRepository, taskRunId);
    }

    private async Task<VideoFileEntity[]> GetFiles()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();
        var files = await scope.ServiceProvider
            .GetRequiredService<VideoRepository>()
            .GetFilesAsync(_videoId, _userId, transaction, CancellationToken.None);

        await transaction.CommitAsync();
        return files.ToArray();
    }

    private static async Task RunFfmpeg(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("ffmpeg") { RedirectStandardError = true };
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-y");
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0, error);
    }
}
