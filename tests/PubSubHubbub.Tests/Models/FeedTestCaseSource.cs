using System.Collections;
using System.Collections.Generic;
using NodaTime;
using NUnit.Framework;
using PubSubHubbub.Models;

namespace PubSubHubbub.Tests.Models;

public sealed class FeedTestCaseSource : IEnumerable<TestCaseData<string, Feed>>
{
    /// <inheritdoc />
    public IEnumerator<TestCaseData<string, Feed>> GetEnumerator()
    {
        var feedUpdated = new OffsetDateTime(new LocalDateTime(2015, 04, 01, 19, 05, 24, 552), Offset.Zero);
        feedUpdated = feedUpdated.PlusNanoseconds(394234);

        var entryUpdated = new OffsetDateTime(new LocalDateTime(2015, 03, 09, 19, 05, 24, 552), Offset.Zero);
        entryUpdated = entryUpdated.PlusNanoseconds(394234);

        yield return new(
            // lang=xml
            """
            <feed xmlns:yt="http://www.youtube.com/xml/schemas/2015"
                     xmlns="http://www.w3.org/2005/Atom">
              <link rel="hub" href="https://pubsubhubbub.appspot.com"/>
              <link rel="self" href="https://www.youtube.com/xml/feeds/videos.xml?channel_id=CHANNEL_ID"/>
              <title>YouTube video feed</title>
              <updated>2015-04-01T19:05:24.552394234+00:00</updated>
              <entry>
                <id>yt:video:VIDEO_ID</id>
                <yt:videoId>VIDEO_ID</yt:videoId>
                <yt:channelId>CHANNEL_ID</yt:channelId>
                <title>Video title</title>
                <link rel="alternate" href="http://www.youtube.com/watch?v=VIDEO_ID"/>
                <author>
                 <name>Channel title</name>
                 <uri>http://www.youtube.com/channel/CHANNEL_ID</uri>
                </author>
                <published>2015-03-06T21:40:57+00:00</published>
                <updated>2015-03-09T19:05:24.552394234+00:00</updated>
              </entry>
            </feed>
            """,
            new()
            {
                Links =
                [
                    new Link
                    {
                        Relation = "hub",
                        Uri = "https://pubsubhubbub.appspot.com",
                    },
                    new Link
                    {
                        Relation = "self",
                        Uri = "https://www.youtube.com/xml/feeds/videos.xml?channel_id=CHANNEL_ID",
                    },
                ],
                Title = "YouTube video feed",
                Updated = feedUpdated,
                Entry = new Entry
                {
                    Id = "yt:video:VIDEO_ID",
                    VideoId = "VIDEO_ID",
                    ChannelId = "CHANNEL_ID",
                    Title = "Video title",
                    Link = new Link
                    {
                        Relation = "alternate",
                        Uri = "http://www.youtube.com/watch?v=VIDEO_ID",
                    },
                    Author = new()
                    {
                        Name = "Channel title",
                        Uri = "http://www.youtube.com/channel/CHANNEL_ID",
                    },
                    Published = new OffsetDateTime(new LocalDateTime(2015, 03, 06, 21, 40, 57), Offset.Zero),
                    Updated = entryUpdated,
                }
            })
        {
            TestName = "New or updated video",
        };

        yield return new(
            // lang=xml
            """
            <?xml version='1.0' encoding='UTF-8'?>
            <feed xmlns:at="http://purl.org/atompub/tombstones/1.0" xmlns="http://www.w3.org/2005/Atom"><at:deleted-entry ref="yt:video:VIDEO_ID" when="2026-04-07T02:10:06.889128+00:00">
                <link href="https://www.youtube.com/watch?v=VIDEO_ID"/>
                <at:by>
                    <name>Channel title</name>
                    <uri>https://www.youtube.com/channel/CHANNEL_ID</uri>
                </at:by>
            </at:deleted-entry></feed>
            """,
            new()
            {
                DeletedEntry = new()
                {
                    VideoId = "yt:video:VIDEO_ID",
                    When = "2026-04-07T02:10:06.889128+00:00",
                    Link = new() { Uri = "https://www.youtube.com/watch?v=VIDEO_ID" },
                    By = new()
                    {
                        Name = "Channel title",
                        Uri = "https://www.youtube.com/channel/CHANNEL_ID"
                    },
                }
            })
        {
            TestName = "Deleted video",
        };
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
