using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tubeshade.Data.Media;
using Tubeshade.Server.Services.Ffmpeg;

namespace Tubeshade.Server.Tests.Services.Ffmpeg;

public sealed class FfmpegExceptionTestCaseSource : IEnumerable<TestCaseData<Func<FfmpegService, Task>, string>>
{
    /// <inheritdoc />
    public IEnumerator<TestCaseData<Func<FfmpegService, Task>, string>> GetEnumerator()
    {
        yield return new(
            service => service.AnalyzeFile("foo.jpg", CancellationToken.None).AsTask(),
            "foo.jpg: No such file or directory")
        {
            TestName = nameof(FfmpegService.AnalyzeFile)
        };

        yield return new(
            service => service
                .RemuxFile(
                    "foo.mp4",
                    VideoContainerType.Mp4,
                    new ProbeResponse
                    {
                        Streams =
                        [
                            new Stream
                            {
                                Index = 0,
                                CodecName = "unknown",
                                CodecType = "audio",
                                RFrameRate = "",
                                AvgFrameRate = "",
                                Disposition = null!,
                            },
                            new Stream
                            {
                                Index = 1,
                                CodecName = "",
                                CodecType = "video",
                                RFrameRate = "",
                                AvgFrameRate = "",
                                Disposition = null!,
                            },
                        ]
                    },
                    CancellationToken.None)
                .AsTask(),
            OperatingSystem.IsWindows()
                ? "foo.mp4: No such file or directory"
                : "[in#0 @ 0x*] Error opening input: No such file or directory\nError opening input file foo.mp4.\nError opening input files: No such file or directory")
        {
            TestName = nameof(FfmpegService.RemuxFile)
        };

        yield return new(
            service => service.CombineStreams(
                "foo.video.mp4",
                "foo.audio.webm",
                "foo.mp4",
                CancellationToken.None),
            OperatingSystem.IsWindows()
                ? "file:foo.video.mp4: No such file or directory"
                : "[in#0 @ 0x*] Error opening input: No such file or directory\nError opening input file file:foo.video.mp4.\nError opening input files: No such file or directory")
        {
            TestName = nameof(FfmpegService.CombineStreams)
        };
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
