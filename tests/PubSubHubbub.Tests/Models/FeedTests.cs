using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using FluentAssertions;
using NUnit.Framework;
using PubSubHubbub.Models;

namespace PubSubHubbub.Tests.Models;

public sealed class FeedTests
{
    [TestCaseSource(typeof(FeedTestCaseSource))]
    public void Deserialize(string xml, Feed expected)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var feed = FeedSerializer.Deserialize(stream);

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
