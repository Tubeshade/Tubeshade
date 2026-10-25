using AwesomeAssertions;
using NodaTime;
using NUnit.Framework;
using Tubeshade.Server.Services;
using YoutubeDLSharp.Metadata;

namespace Tubeshade.Server.Tests.Services;

public sealed class VideoDataExtensionsTests
{
    private static readonly VideoData Data = new()
    {
        Id = "test",
        Title = "Test video",
        Formats =
        [
            Format("251", "https://rr1---sn-example.googlevideo.com/videoplayback?expire=1792972232&ei=abc&itag=251"),
            Format("399", "https://rr1---sn-example.googlevideo.com/videoplayback?ei=abc&expire=1792972000&itag=399"),
            Format("96",
                "https://manifest.googlevideo.com/api/manifest/hls_playlist/expire/1792971000/ei/abc/itag/96/playlist/index.m3u8"),
            Format("sb0", "https://i.ytimg.com/sb/abc/storyboard3_L0/default.jpg?sqp=abc"),
        ],
    };

    [TestCase(new[] { "251" }, 1792972232, TestName = "Query parameter")]
    [TestCase(new[] { "96" }, 1792971000, TestName = "Path segment")]
    [TestCase(new[] { "251", "399" }, 1792972000, TestName = "Earliest of multiple formats")]
    public void GetExpiry(string[] formatIds, long expectedSeconds)
    {
        Data.GetExpiry(formatIds).Should().Be(Instant.FromUnixTimeSeconds(expectedSeconds));
    }

    [TestCase("251", "sb0", TestName = "Format without expiry")]
    [TestCase("251", "137", TestName = "Unknown format")]
    public void GetExpiry_ShouldBeNull(params string[] formatIds)
    {
        Data.GetExpiry(formatIds).Should().BeNull();
    }

    private static FormatData Format(string formatId, string url) => new()
    {
        FormatId = formatId,
        Format = formatId,
        Extension = "mp4",
        Url = url,
    };
}