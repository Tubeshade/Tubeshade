using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
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
        using var process = new CancelableProcess("yt-dlp", string.Join(' ', _args));

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
        process.Output.Should().BeEmpty("standard output should only contain JSON info");
        process.Error.Should()
            .Contain($"[debug] Command-line config: [{string.Join(", ", _args.Select(arg => $"'{arg}'"))}]");
    }

    [Test]
    public async Task RunToCompletion()
    {
        using var process = new CancelableProcess("yt-dlp", string.Join(' ', _args));

        var result = await process.Run();

        using var scope = new AssertionScope();

        result.Should().BeGreaterThan(0);
        process.Output.Should().BeEmpty("standard output should only contain JSON info");
        if (OperatingSystem.IsWindows())
        {
            process.Error.Should().Contain(
                "ERROR: [youtube] njX2bu-_Vw4: Sign in to confirm you�re not a bot. " +
                "Use --cookies-from-browser or --cookies for the authentication. " +
                "See  https://github.com/yt-dlp/yt-dlp/wiki/FAQ#how-do-i-pass-cookies-to-yt-dlp  for how to manually pass cookies. " +
                "Also see  https://github.com/yt-dlp/yt-dlp/wiki/Extractors#exporting-youtube-cookies  for tips on effectively exporting YouTube cookies");
        }
        else
        {
            process.Error.Should().Contain(
                "ERROR: [youtube] njX2bu-_Vw4: Sign in to confirm you’re not a bot. " +
                "Use --cookies-from-browser or --cookies for the authentication. " +
                "See  https://github.com/yt-dlp/yt-dlp/wiki/FAQ#how-do-i-pass-cookies-to-yt-dlp  for how to manually pass cookies. " +
                "Also see  https://github.com/yt-dlp/yt-dlp/wiki/Extractors#exporting-youtube-cookies  for tips on effectively exporting YouTube cookies");
        }
    }

    [Test]
    public async Task FailedToStart_ShouldThrow()
    {
        await FluentActions
            .Awaiting(async () =>
            {
                using var process = new CancelableProcess(Guid.NewGuid().ToString(), string.Join(' ', _args));
                return await process.Run();
            })
            .Should()
            .ThrowExactlyAsync<Win32Exception>();
    }

    [Test]
    public async Task Finalizer_ShouldNotThrow()
    {
        using var process = new CancelableProcess("yt-dlp", string.Join(' ', _args));

        var processStarted = new TaskCompletionSource();
        process.ErrorReceived += (_, _) => processStarted.TrySetResult();

        _ = process.Run();
        await processStarted.Task;

        var finalizer = typeof(CancelableProcess).GetMethod("Finalize", BindingFlags.Instance | BindingFlags.NonPublic);
        finalizer.Should().NotBeNull();

        finalizer!.Invoke(process, null);
    }
}
