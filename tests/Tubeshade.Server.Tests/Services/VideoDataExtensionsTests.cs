using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Tubeshade.Server.Services;
using YoutubeDLSharp.Metadata;

namespace Tubeshade.Server.Tests.Services;

public sealed class VideoDataExtensionsTests
{
    [Test]
    public void GetOrderedThumbnails_ShouldReturnExpected()
    {
        var video = new VideoData
        {
            Id = "",
            Title = "",
            Thumbnails =
            [
                new ThumbnailData { Url = "1", Preference = -7 },
                new ThumbnailData { Url = "2", Width = 100, Preference = -7 },
                new ThumbnailData { Url = "3", Width = 200, Preference = -7 },
                new ThumbnailData { Url = "4", Preference = -5 },
                new ThumbnailData { Url = "5", Width = 1080, Preference = -1 },
                new ThumbnailData { Url = "6", Preference = 0 },
            ],
        };

        var ordered = video.GetOrderedThumbnails();

        ordered.Select(data => data.Url).Should().BeEquivalentTo("6", "5", "4", "3", "2", "1");
    }
}
