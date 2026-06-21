using FluentAssertions;
using NUnit.Framework;
using Tubeshade.Server.Pages.Libraries.Videos;

namespace Tubeshade.Server.Tests.Pages.Libraries.Videos;

public sealed class StringExtensionsTests
{
    private const string VideoId = "njX2bu-_Vw4";

    [TestCase($"youtu.be/{VideoId}?feature=shared", null)]
    [TestCase($"youtu.be/{VideoId}?feature=shared&t=", null)]
    [TestCase($"youtu.be/{VideoId}?t=90", 90)]
    [TestCase($"www.youtube.com/watch?v={VideoId}&t=90s", 90)]
    [TestCase($"youtu.be/{VideoId}?feature=shared&t=2h24m13s", 8_653)]
    [TestCase($"youtu.be/{VideoId}?feature=shared&t=2h24m", 8_640)]
    [TestCase($"youtu.be/{VideoId}?feature=shared&t=2h", 7_200)]
    public void GetTimeParameter_ShouldReturnExpected(string uri, double? expected)
    {
        uri.GetTimeParameter().Should().Be(expected);
    }
}
