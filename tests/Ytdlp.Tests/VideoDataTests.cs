using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Ytdlp.Tests;

public sealed class VideoDataTests
{
    [TestCaseSource(typeof(VideoDataTestCaseSource))]
    public void Test(string json, VideoData expected)
    {
        JsonSerializer
            .Deserialize(json, YtdlpSerializerContext.Default.VideoData)
            .Should()
            .BeEquivalentTo(expected);
    }
}
