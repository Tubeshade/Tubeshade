using System;
using System.Linq;
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

    [TestCase("mp4+opus+eac3", "mp4+opus+eac3", TestName = "MP4 keeps all audio tracks")]
    [TestCase("mp4+opus+opus", "mp4+opus", TestName = "Duplicate audio track")]
    [TestCase("webm+opus+eac3", "webm+opus", TestName = "WebM drops audio it cannot contain")]
    [TestCase("webm+opus+vorbis", "webm+opus+vorbis", TestName = "WebM keeps audio it can contain")]
    [TestCase("webm+eac3", "webm+vorbis", TestName = "WebM replaces audio it cannot contain")]
    [TestCase("webm+eac3+opus", "webm+opus", TestName = "WebM uses selected audio it can contain")]
    [TestCase("muxed+opus+eac3", "muxed", TestName = "Format with its own audio ignores other audio")]
    public void GetSelectedFormats(string formatId, string expectedFormatId)
    {
        var data = new VideoData
        {
            Id = "test",
            Title = "Test video",
            FormatId = formatId,
            Formats =
            [
                Video("mp4", "mp4"),
                Video("webm", "webm"),
                Muxed("muxed"),
                Audio("opus", "opus"),
                Audio("vorbis", "vorbis"),
                Audio("eac3", "ec-3"),
            ],
        };

        string.Join('+', data.GetSelectedFormats().Select(format => format.FormatId)).Should().Be(expectedFormatId);
    }

    [Test]
    public void GetSelectedFormats_ShouldReplaceAudio_WithSameLanguage()
    {
        var data = WebMWithAudioInLanguage("en");

        string.Join('+', data.GetSelectedFormats().Select(format => format.FormatId)).Should().Be("webm+opus-en");
    }

    [Test]
    public void GetSelectedFormats_ShouldThrow_WhenNoCompatibleAudioInSameLanguage()
    {
        var data = WebMWithAudioInLanguage("lv");

        FluentActions
            .Invoking(data.GetSelectedFormats)
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*'lv'*");
    }

    [Test]
    public void GetTotalSize_ShouldSumFileAndApproximateSizes()
    {
        FormatData[] formats = [Sized("video", 100, null), Sized("audio", null, 20)];

        formats.GetTotalSize().Should().Be(120);
    }

    [Test]
    public void GetTotalSize_ShouldBeNull_WhenAnyFormatHasNoSize()
    {
        FormatData[] formats = [Sized("video", null, null), Sized("audio", 20, null)];

        formats.GetTotalSize().Should().BeNull();
    }

    [TestCase("@FortNine", "https://www.youtube.com/@FortNine", TestName = "Channel handle")]
    [TestCase("UCNSMdQtn1SuFzCZjfK2C7dQ", "https://www.youtube.com/@FortNine/videos", TestName = "Channel handle videos tab")]
    [TestCase("UCNSMdQtn1SuFzCZjfK2C7dQ", "https://www.youtube.com/@FortNine/playlists", TestName = "Channel handle playlists tab")]
    [TestCase("UCNSMdQtn1SuFzCZjfK2C7dQ", "https://www.youtube.com/channel/UCNSMdQtn1SuFzCZjfK2C7dQ", TestName = "Channel id")]
    [TestCase("LinusTechTips", "https://www.youtube.com/c/LinusTechTips", TestName = "Channel custom url")]
    [TestCase("LinusTechTips", "https://www.youtube.com/user/LinusTechTips", TestName = "Channel legacy username")]
    [TestCase("LinusTechTips", "https://www.youtube.com/LinusTechTips", TestName = "Channel vanity url")]
    public void IsPlaylist_Channel_ShouldBeFalse(string id, string webpageUrl)
    {
        var data = new VideoData
        {
            ResultType = MetadataType.Playlist,
            Id = id,
            Title = "FortNine",
            ChannelId = "UCNSMdQtn1SuFzCZjfK2C7dQ",
            WebpageUrl = webpageUrl,
        };

        data.IsPlaylist().Should().BeFalse();
    }

    [Test]
    public void IsPlaylist_Playlist_ShouldBeTrue()
    {
        var data = new VideoData
        {
            ResultType = MetadataType.Playlist,
            Id = "PLzH6n4zXuckqbjBDsA_Hc-q5hI2lGml1F",
            Title = "Mike Pound on Computerphile",
            ChannelId = "UC9-y-6csu5WGm29I7JiwpnA",
            WebpageUrl = "https://www.youtube.com/playlist?list=PLzH6n4zXuckqbjBDsA_Hc-q5hI2lGml1F",
        };

        data.IsPlaylist().Should().BeTrue();
    }

    [Test]
    public void IsPlaylist_Video_ShouldBeFalse()
    {
        var data = new VideoData
        {
            ResultType = MetadataType.Video,
            Id = "PE0p2iHwjVc",
            Title = "Test video",
            ChannelId = "UC9-y-6csu5WGm29I7JiwpnA",
            WebpageUrl = "https://www.youtube.com/watch?v=PE0p2iHwjVc",
        };

        data.IsPlaylist().Should().BeFalse();
    }

    private static FormatData Format(string formatId, string url) => new()
    {
        FormatId = formatId,
        Format = formatId,
        Extension = "mp4",
        Url = url,
    };

    private static FormatData Sized(string formatId, long? fileSize, long? approximateFileSize) => new()
    {
        FormatId = formatId,
        Format = formatId,
        Extension = "mp4",
        Url = "https://example.com",
        FileSize = fileSize,
        ApproximateFileSize = approximateFileSize,
    };

    private static FormatData Video(string formatId, string extension) => new()
    {
        FormatId = formatId,
        Format = formatId,
        Extension = extension,
        Url = "https://example.com",
        Resolution = "1920x1080",
        AudioCodec = "none",
    };

    private static VideoData WebMWithAudioInLanguage(string language) => new()
    {
        Id = "test",
        Title = "Test video",
        FormatId = $"webm+aac-{language}",
        Formats =
        [
            Video("webm", "webm"),
            Audio("opus-en", "opus", "en"),
            Audio("aac-en", "mp4a.40.2", "en"),
            Audio("aac-lv", "mp4a.40.2", "lv"),
        ],
    };

    private static FormatData Muxed(string formatId) => new()
    {
        FormatId = formatId,
        Format = formatId,
        Extension = "mp4",
        Url = "https://example.com",
        Resolution = "1920x1080",
        AudioCodec = "mp4a.40.2",
    };

    private static FormatData Audio(string formatId, string codec, string? language = null) => new()
    {
        Language = language,
        FormatId = formatId,
        Format = formatId,
        Extension = "m4a",
        Url = "https://example.com",
        Resolution = "audio only",
        AudioCodec = codec,
    };
}
