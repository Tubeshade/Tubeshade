using System;
using System.IO;
using System.Xml.Serialization;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NUnit.Framework;
using PubSubHubbub.Models;

namespace PubSubHubbub.Tests.Models;

public class FeedTests
{
    [Test]
    public void Deserialize()
    {
        var xml =
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
            """u8;

        var feedUpdated = new OffsetDateTime(new LocalDateTime(2015, 04, 01, 19, 05, 24, 552), Offset.Zero);
        feedUpdated = feedUpdated.PlusNanoseconds(394234);

        var entryUpdated = new OffsetDateTime(new LocalDateTime(2015, 03, 09, 19, 05, 24, 552), Offset.Zero);
        entryUpdated = entryUpdated.PlusNanoseconds(394234);

        var expected = new Feed
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
        };

        using var memoryStream = new MemoryStream(xml.ToArray());
        var feed = FeedSerializer.Deserialize(memoryStream);

        using var scope = new AssertionScope();
        feed.Should().BeEquivalentTo(expected);
    }

    private static class FeedSerializer
    {
        private static readonly XmlSerializer Serializer = new(typeof(Feed));

        public static Feed Deserialize(Stream stream)
        {
            if (Serializer.Deserialize(stream) is not Feed feed)
            {
                throw new ArgumentException("Stream does not contain a valid feed", nameof(stream));
            }

            return feed;
        }
    }
}
