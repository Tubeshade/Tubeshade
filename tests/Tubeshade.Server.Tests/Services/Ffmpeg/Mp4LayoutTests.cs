using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Tubeshade.Server.Configuration;
using Tubeshade.Server.Services.Ffmpeg;

namespace Tubeshade.Server.Tests.Services.Ffmpeg;

public sealed class Mp4LayoutTests
{
    private readonly FfmpegService _service = new(
        NullLogger<FfmpegService>.Instance,
        new MockOptionsMonitor<YtdlpOptions>(options =>
        {
            options.FfmpegPath = "ffmpeg";
            options.FfprobePath = "ffprobe";
        }));

    private DirectoryInfo _directory = null!;
    private string _videoPath = null!;
    private string _audioPath = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _directory = Directory.CreateTempSubdirectory("tubeshade-mp4-layout");
        _videoPath = Path.Combine(_directory.FullName, "video.mp4");
        _audioPath = Path.Combine(_directory.FullName, "audio.m4a");

        await RunFfmpeg("-f", "lavfi", "-i", "testsrc=duration=5:size=320x240:rate=25", "-c:v", "mpeg4", _videoPath);
        await RunFfmpeg("-f", "lavfi", "-i", "sine=duration=5", "-c:a", "aac", _audioPath);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _directory.Delete(true);

    [Test]
    public async Task IndexAtEnd_ShouldNotBeStreamable()
    {
        var path = Path.Combine(_directory.FullName, "index-at-end.mp4");
        await RunFfmpeg("-i", _videoPath, "-i", _audioPath, "-c", "copy", path);

        var streamable = await Mp4Layout.IsStreamable(path, CancellationToken.None);

        streamable.Should().BeFalse();
    }

    [Test]
    public async Task Fragmented_ShouldNotBeStreamable()
    {
        var path = Path.Combine(_directory.FullName, "fragmented.mp4");
        await _service.CombineStreams(_videoPath, _audioPath, path, CancellationToken.None);

        var streamable = await Mp4Layout.IsStreamable(path, CancellationToken.None);

        streamable.Should().BeFalse();
    }

    [Test]
    public async Task FragmentedWithIndex_ShouldNotBeStreamable()
    {
        var path = Path.Combine(_directory.FullName, "fragmented-with-index.mp4");
        await RunFfmpeg("-i", _videoPath, "-i", _audioPath, "-c", "copy", "-movflags", "+frag_keyframe", path);

        var streamable = await Mp4Layout.IsStreamable(path, CancellationToken.None);

        streamable.Should().BeFalse();
    }

    [Test]
    public async Task Copy_ShouldBeStreamable()
    {
        var fragmentedPath = Path.Combine(_directory.FullName, "fragmented-copy.mp4");
        var path = Path.Combine(_directory.FullName, "copy.mp4");
        await _service.CombineStreams(_videoPath, _audioPath, fragmentedPath, CancellationToken.None);
        await _service.Copy(fragmentedPath, path, CancellationToken.None);

        var streamable = await Mp4Layout.IsStreamable(path, CancellationToken.None);

        streamable.Should().BeTrue();
    }

    [Test]
    public async Task Truncated_ShouldThrow()
    {
        var path = Path.Combine(_directory.FullName, "truncated.mp4");
        await File.WriteAllBytesAsync(path, [0, 0, 0, 16, (byte)'f', (byte)'t']);

        await FluentActions
            .Awaiting(async () => await Mp4Layout.IsStreamable(path, CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<EndOfStreamException>();
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
