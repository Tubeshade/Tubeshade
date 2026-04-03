using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using Tubeshade.Server.Pages.Libraries.Videos;

namespace Tubeshade.Server.Tests.Pages.Libraries.Videos;

public sealed class VideoTests
{
    private const string VideoId = "njX2bu-_Vw4";

    private readonly Regex _regex = Video.YoutubeVideoUrlRegex();

    [TestCaseSource(nameof(VideoUrlNewLineTestCases))]
    public void YoutubeVideoUrlRegex_ShouldMatchExpected(string text, string url)
    {
        var match = _regex.Match(text);

        using var scope = new AssertionScope();
        match.Groups["id"].Value.Should().Be(VideoId);
        match.Value.Should().Be(url);
    }

    [TestCase($"notyoutube.com/watch?v={VideoId}")]
    [TestCase($"www.notyoutube.com/watch?v={VideoId}")]
    [TestCase($"https://www.notyoutube.com/watch?v={VideoId}")]
    public void YoutubeVideoUrlRegex_ShouldNotMatch(string url)
    {
        _regex.IsMatch(url).Should().BeFalse();
    }

    private static IEnumerable<TestCaseData<string, string>> VideoUrlNewLineTestCases()
    {
        foreach (var url in Urls())
        {
            yield return new(url, url) { TestName = $"Plain {url}" };

            yield return new(
                $"""
                 foo

                 {url}

                 bar
                 """,
                url) { TestName = $"On separate line {url}" };

            yield return new(
                $"""
                 foo

                 foo {url} bar

                 bar
                 """,
                url) { TestName = $"Within text {url}" };
        }
    }

    private static IEnumerable<string> Urls()
    {
        string[] videoUrls =
        [
            $"youtu.be/{VideoId}?feature=shared",
            $"www.youtube.com/watch?v={VideoId}",
            $"youtube.com/watch?v={VideoId}",
            $"www.youtube.com/watch?v={VideoId}&feature=feedrec_grec_index",
            $"www.youtube.com/v/{VideoId}?fs=1&amp;hl=en_US&amp;rel=0",
            $"www.youtube.com/watch?v={VideoId}#t=0m10s",
            $"www.youtube.com/embed/{VideoId}?rel=0",
            $"youtu.be/{VideoId}",
        ];

        foreach (var url in videoUrls)
        {
            yield return url;
            yield return $"https://{url}";
            yield return $"http://{url}";
        }
    }
}
