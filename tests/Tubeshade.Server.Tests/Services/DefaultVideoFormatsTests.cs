using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NUnit.Framework;
using Tubeshade.Server.Services;
using YoutubeDLSharp.Options;

namespace Tubeshade.Server.Tests.Services;

[Parallelizable(ParallelScope.All)]
public sealed class DefaultVideoFormatsTests
{
    private readonly Ytdlp.Ytdlp _ytdlp = new("yt-dlp");

    [TestCase(0, "video-1080+opus-stereo+eac3")]
    [TestCase(1, "video-720+opus-stereo+eac3")]
    public async Task ShouldSelectPlayableAudio_BeforeSurroundAudio(int selectorIndex, string expectedFormatId)
    {
        var formatId = await SelectFormat(selectorIndex, Audio("opus-stereo", "opus", 2), Audio("ac3", "ac-3", 6),
            Audio("eac3", "ec-3", 6));

        formatId.Should().Be(expectedFormatId);
    }

    [TestCase(0, "video-1080+opus-stereo")]
    [TestCase(1, "video-720+opus-stereo")]
    public async Task ShouldSelectSingleAudio_WhenBestAudioIsPlayable(int selectorIndex, string expectedFormatId)
    {
        var formatId = await SelectFormat(selectorIndex, Audio("opus-stereo", "opus", 2), Audio("aac", "mp4a.40.2", 2));

        formatId.Should().Be(expectedFormatId);
    }

    [TestCase(0, "video-1080+aac+eac3")]
    [TestCase(1, "video-720+aac+eac3")]
    public async Task ShouldPreferPlayableOriginalAudio_OverDubbedAudio(int selectorIndex, string expectedFormatId)
    {
        var formatId = await SelectFormat(
            selectorIndex,
            Audio("opus-dub", "opus", 2),
            Audio("aac", "mp4a.40.2", 2, "[en] original (default)"),
            Audio("eac3", "ec-3", 6, "[en] original (default)"));

        formatId.Should().Be(expectedFormatId);
    }

    [TestCase(0, "video-1080+eac3")]
    [TestCase(1, "video-720+eac3")]
    public async Task ShouldFallBackToUnplayableAudio_WhenNoOtherAudioExists(int selectorIndex, string expectedFormatId)
    {
        var formatId = await SelectFormat(selectorIndex, Audio("eac3", "ec-3", 6));

        formatId.Should().Be(expectedFormatId);
    }

    [TestCase(0, "video-1080+eac3")]
    [TestCase(1, "video-720+eac3")]
    public async Task ShouldKeepOriginalAudio_OverPlayableDubbedAudio(int selectorIndex, string expectedFormatId)
    {
        var formatId = await SelectFormat(
            selectorIndex,
            Audio("opus-dub", "opus", 2),
            Audio("eac3", "ec-3", 6, "[en] original (default)"));

        formatId.Should().Be(expectedFormatId);
    }

    [Test]
    public async Task ShouldNotSelectSeparateAudio_ForFormatWithItsOwnAudio()
    {
        var muxed = new
        {
            format_id = "muxed-720",
            url = "https://example.com/muxed.mp4",
            ext = "mp4",
            width = 1280,
            height = 720,
            fps = 30,
            vcodec = "avc1",
            acodec = "mp4a.40.2",
        };

        var formatId = await SelectFormat(1, muxed, Audio("opus-stereo", "opus", 2), Audio("eac3", "ec-3", 6));

        formatId.Should().Be("muxed-720");
    }

    private async Task<string?> SelectFormat(int selectorIndex, params object[] audioFormats)
    {
        var infoJsonFilePath = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            $"{TestContext.CurrentContext.Test.ID}.info.json");

        var infoJson = new
        {
            id = "test",
            title = "Test video",
            extractor = "generic",
            extractor_key = "Generic",
            webpage_url = "https://example.com/video",
            formats = new[] { Video(720), Video(1080) }.Concat(audioFormats),
        };

        await File.WriteAllTextAsync(infoJsonFilePath, JsonSerializer.Serialize(infoJson));

        var options = new OptionSet
        {
            IgnoreConfig = true,
            DumpSingleJson = true,
            LoadInfoJson = infoJsonFilePath,
            Format = YoutubeIndexingService.DefaultVideoFormats[selectorIndex],
            AudioMultistreams = true,
        };

        var result = await _ytdlp.FetchAsync(string.Empty, options, CancellationToken.None);
        result.Success.Should().BeTrue(string.Join('\n', result.ErrorOutput));

        return string.Join('+', result.Data!.GetSelectedFormats().Select(format => format.FormatId));
    }

    private static object Video(int height) => new
    {
        format_id = $"video-{height}",
        url = $"https://example.com/{height}.mp4",
        ext = "mp4",
        width = height * 16 / 9,
        height,
        fps = 30,
        vcodec = "avc1",
        acodec = "none",
    };

    private static object Audio(string id, string codec, int channels, string note = "[en]") => new
    {
        format_id = id,
        url = $"https://example.com/{id}.m4a",
        ext = "m4a",
        vcodec = "none",
        acodec = codec,
        audio_channels = channels,
        format_note = note,
    };
}