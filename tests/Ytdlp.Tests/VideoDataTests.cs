using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using Ytdlp.Tests.Videos;

namespace Ytdlp.Tests;

public sealed class VideoDataTests
{
    [TestCaseSource(typeof(VideoDataTestCaseSource))]
    public void Deserialize(string json, VideoData expected)
    {
        JsonSerializer
            .Deserialize(json, YtdlpSerializerContext.Default.VideoData)
            .Should()
            .BeEquivalentTo(expected);
    }
}
