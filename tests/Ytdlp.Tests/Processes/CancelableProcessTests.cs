using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using NUnit.Framework;
using Ytdlp.Processes;

namespace Ytdlp.Tests.Processes;

[Parallelizable(ParallelScope.All)]
public sealed class CancelableProcessTests
{
    private readonly string[] _args = ["--dump-json", "--verbose", "https://www.youtube.com/watch?v=njX2bu-_Vw4"];

    [Test]
    public async Task Run_ShouldRespectCancellation()
    {
        using var source = new CancellationTokenSource();
        await using var process = new CancelableProcess("yt-dlp", _args);

        var processStarted = new TaskCompletionSource();
        process.ErrorReceived += (_, _) => processStarted.TrySetResult();

        var processTask = process.Run(source.Token);

        await processStarted.Task;
        await source.CancelAsync();

        var result = (await FluentActions
                .Awaiting(() => processTask)
                .Should()
                .CompleteWithinAsync(TimeSpan.FromSeconds(1)))
            .Subject;

        using var scope = new AssertionScope();

        result.Should().BeGreaterThan(0);
        process.OutputLines.Should().BeEmpty("standard output should only contain JSON info");
        process.ErrorLines.Should()
            .Contain($"[debug] Command-line config: [{string.Join(", ", _args.Select(arg => $"'{arg}'"))}]");
    }

    [Test]
    public async Task RunToCompletion()
    {
        await using var process = new CancelableProcess("yt-dlp", _args);

        var result = await process.Run();
        await TestContext.Out.WriteLineAsync(string.Join(Environment.NewLine, process.ErrorLines));

        using var scope = new AssertionScope();

        result.Should().BeGreaterThan(0);
        process.OutputLines.Should().BeEmpty("standard output should only contain JSON info");

        // The reason after "not a bot." comes from YouTube and differs between environments
        process
            .ErrorLines.Should()
            .ContainSingle(line => line.StartsWith("ERROR: "))
            .Subject.Should()
            .StartWith("ERROR: [youtube] njX2bu-_Vw4: Sign in to confirm you")
            .And.Contain(" not a bot. ").And
            .EndWith(
                "Use --cookies-from-browser or --cookies for the authentication. " +
                "See  https://github.com/yt-dlp/yt-dlp/wiki/FAQ#how-do-i-pass-cookies-to-yt-dlp  for how to manually pass cookies. " +
                "Also see  https://github.com/yt-dlp/yt-dlp/wiki/Extractors#exporting-youtube-cookies  for tips on effectively exporting YouTube cookies");
    }

    [Test]
    public async Task FailedToStart_ShouldThrow()
    {
        await FluentActions
            .Awaiting(async () =>
            {
                await using var process = new CancelableProcess(Guid.NewGuid().ToString(), _args);
                return await process.Run();
            })
            .Should()
            .ThrowExactlyAsync<Win32Exception>();
    }

    [Test]
    public async Task DisposeAsync_ShouldStopProcess()
    {
        var process = new CancelableProcess("yt-dlp", _args);

        var processStarted = new TaskCompletionSource();
        process.ErrorReceived += (_, _) => processStarted.TrySetResult();

        var processTask = process.Run();
        await processStarted.Task;

        await FluentActions
            .Awaiting(() => process.DisposeAsync().AsTask())
            .Should()
            .CompleteWithinAsync(TimeSpan.FromSeconds(1));

        processTask.IsCompletedSuccessfully.Should().BeTrue();
        processTask.Result.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task DisposeAsync_NotStarted_ShouldNotThrow()
    {
        var process = new CancelableProcess("yt-dlp", _args);

        await FluentActions
            .Awaiting(() => process.DisposeAsync().AsTask())
            .Should()
            .NotThrowAsync();
    }

    [Test]
    [Platform("Linux")]
    public async Task Run_ShouldStopChildProcesses()
    {
        using var source = new CancellationTokenSource();

        // Ignoring SIGTERM is inherited by the child process, which also keeps standard output and error open
        await using var process = new CancelableProcess("sh", ["-c", "\"trap '' TERM; echo started >&2; sleep 60 & wait\""]);

        var processStarted = new TaskCompletionSource();
        process.ErrorReceived += (_, _) => processStarted.TrySetResult();

        var processTask = process.Run(source.Token);

        await processStarted.Task;
        await source.CancelAsync();

        (await FluentActions.Awaiting(() => processTask).Should().CompleteWithinAsync(TimeSpan.FromSeconds(2)))
            .Subject
            .Should()
            .BeGreaterThan(0);
    }
}
