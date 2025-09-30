using System;
using System.Xml.Serialization;
using NodaTime;

namespace PubSubHubbub.Models;

[Serializable]
public sealed class Entry
{
    [XmlElement("id")]
    public required string Id { get; init; }

    [XmlElement("videoId", Namespace = "http://www.youtube.com/xml/schemas/2015")]
    public required string VideoId { get; init; }

    [XmlElement("channelId", Namespace = "http://www.youtube.com/xml/schemas/2015")]
    public required string ChannelId { get; init; }

    [XmlElement("title")]
    public required string Title { get; init; }

    [XmlElement("link")]
    public required Link Link { get; init; }

    [XmlElement("author")]
    public required Author Author { get; init; }

    [XmlElement("published")]
    public required OffsetDateTime Published { get; init; }

    [XmlElement("updated")]
    public required OffsetDateTime Updated { get; init; }
}
