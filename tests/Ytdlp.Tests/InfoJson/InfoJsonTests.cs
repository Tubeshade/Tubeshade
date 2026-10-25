using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using NUnit.Framework;
using YoutubeDLSharp.Options;

namespace Ytdlp.Tests.InfoJson;

[Parallelizable(ParallelScope.All)]
public sealed class InfoJsonTests
{
    private readonly Ytdlp _ytdlp = new("yt-dlp");

    [TestCase("bv*[height<=720]+ba", "video-720+audio")]
    [TestCase("bv+ba", "video-1080+audio")]
    [TestCase("bv[height>5000]", null, TestName = "No matching formats")]
    public async Task SelectFormat_ShouldNotReuseFetchedFormat(string format, string? expectedFormatId)
    {
        var token = CancellationToken.None;

        // test cases run in parallel, so each needs its own file
        var infoJsonFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            $"{TestContext.CurrentContext.Test.ID}.info.json");
        var options = new OptionSet
        {
            IgnoreConfig = true,
            IgnoreNoFormatsError = true,
            DumpSingleJson = true,
            LoadInfoJson = GetRelativePath("video.json"),
        };

        // the fetched data contains the default selected format "video-1080+audio"
        var fetchResult = await _ytdlp.FetchAsync(string.Empty, options, infoJsonFilePath, token);
        fetchResult.Success.Should().BeTrue(string.Join('\n', fetchResult.ErrorOutput));
        fetchResult.Data!.FormatId.Should().Be("video-1080+audio");

        options.LoadInfoJson = infoJsonFilePath;
        options.Format = format;
        var selectResult = await _ytdlp.FetchAsync(string.Empty, options, token);
        selectResult.Success.Should().BeTrue(string.Join('\n', selectResult.ErrorOutput));

        using var scope = new AssertionScope();
        selectResult.Data!.FormatId.Should().Be(expectedFormatId);
        selectResult.Data.Formats.Should().HaveCount(3);
    }

    private static string GetRelativePath(string fileName, [CallerFilePath] string path = "")
    {
        var directory = Path.GetDirectoryName(path)!;
        return Path.Combine(directory, fileName);
    }
}
