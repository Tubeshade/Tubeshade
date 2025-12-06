using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using NodaTime;
using NUnit.Framework;

namespace Ytdlp.Tests;

public sealed class VideoDataTestCaseSource : IEnumerable<TestCaseData<string, VideoData>>
{
    private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    /// <inheritdoc />
    public IEnumerator<TestCaseData<string, VideoData>> GetEnumerator()
    {
        var stream = Assembly.GetManifestResourceStream(typeof(VideoDataTestCaseSource), "njX2bu-_Vw4.info.json")!;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        yield return new(
            json,
            new VideoData
            {
                Type = "video",
                Id = "njX2bu-_Vw4",
                Title = "2020 LG OLED l  The Black 4K HDR 60fps",
                // Formats = [],
                Url = null,
                Extension = "mp4",
                Format = "701 - 3840x2160 (2160p60 HDR)+258 - audio only (high)",
                PlayerUrl = null,
                Direct = null,
                AlternativeTitle = null,
                DisplayId = "njX2bu-_Vw4",
                // Thumbnails = [],
                Thumbnail = "https://i.ytimg.com/vi/njX2bu-_Vw4/maxresdefault.jpg",
                Description =
                    "The Power of SELF-LIT PiXELS\n\nMeet all new LG OLED with 100 million of SELF-LIT PiXELS. \nWhen every pixel lights by itself, what you see becomes more \nExpressive, \nRealistic, \nResponsive,\nand Artistic.\n\nThe Power of SELF-LIT PiXELS\n\nLearn more : https://www.lg.com/uk/oled-tvs  \n\n#LGOLED, #Black, #4KHDR, #SELFLIT, #4KHDR60fps,  #SELFLITPiXELS, #SELFLITOLED",
                Uploader = "LG Global",
                Channel = "LG Global",
                ChannelId = "UC2SIWgqcys7Gcb6JxsFTm1Q",
                ChannelUrl = "https://www.youtube.com/channel/UC2SIWgqcys7Gcb6JxsFTm1Q",
                DurationInSeconds = 127,
                Duration = Duration.FromSeconds(127),
                Timestamp = 1589531938,
                Categories = ["Science & Technology"],
                Tags =
                [
                    "LGTV",
                    "4KTV",
                    "LG4K",
                    "LG8K",
                    "8KOLEDTV",
                    "8KTV",
                    "Real8K",
                    "BestTV",
                    "BestSmartTV",
                    "BestOLEDTV",
                    "Best4KTV",
                    "Best8KTV LGOLEDTV",
                    "OLED",
                    "OLEDTV",
                    "LGOLED",
                    "CES2020",
                    "selflitpixels",
                    "selflitpixel",
                    "selflitlgoled",
                    "BigscreenTV",
                    "LargeScreenTV",
                    "77inchTV",
                    "77LGTV",
                    "77LGOLED",
                    "88inchTV",
                    "88LGTV",
                    "88LGOLED",
                    "newTV",
                    "2020newTV",
                    "LG2020tv",
                    "smarttv",
                    "PictureQuality",
                    "SoundQuality",
                    "AI",
                    "ArtificialIntelligence",
                    "LGThinQ",
                    "LGThinQAI",
                    "Processor",
                    "AIProcessor",
                    "IntelligentProcessor",
                    "a9Gen3IntelligentProcessor",
                    "DeepLearning",
                    "DeepLearningTechnology",
                    "BestWatchingExperience"
                ],
                LikeCount = 157104,
                WebpageUrl = "https://www.youtube.com/watch?v=njX2bu-_Vw4",
                Availability = "public",
                LiveStatus = "not_live",
            });
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
