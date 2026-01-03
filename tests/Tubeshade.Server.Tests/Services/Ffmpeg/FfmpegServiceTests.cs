using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Tubeshade.Server.Configuration;
using Tubeshade.Server.Services.Ffmpeg;

namespace Tubeshade.Server.Tests.Services.Ffmpeg;

public sealed class FfmpegServiceTests
{
    private readonly FfmpegService _service = new(
        NullLogger<FfmpegService>.Instance,
        new MockOptionsMonitor<YtdlpOptions>(options =>
        {
            options.FfmpegPath = "ffmpeg";
            options.FfprobePath = "ffprobe";
        }));

    [TestCase("thumbnail.jpg", "mjpeg", 1280, 720)]
    [TestCase("thumbnail.webp", "webp", 1280, 720)]
    public async Task AnalyzeThumbnail_ShouldContainResolution(string name, string codec, int width, int height)
    {
        var response = await _service.AnalyzeFile(name.GetRelativePath(), CancellationToken.None);

        var stream = response.Streams.Should().ContainSingle().Subject;

        using var scope = new AssertionScope();
        stream.CodecName.Should().Be(codec);
        stream.Width.Should().Be(width);
        stream.Height.Should().Be(height);
    }

    [TestCaseSource(typeof(FfmpegExceptionTestCaseSource))]
    public async Task ShouldThrowExpectedException(Func<FfmpegService, Task> function, string expectedMessage)
    {
        await FluentActions
            .Awaiting(async () => await function(_service))
            .Should()
            .ThrowExactlyAsync<Exception>()
            .WithMessage(expectedMessage);
    }
}
