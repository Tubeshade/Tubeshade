using System;
using System.Xml.Serialization;
using NodaTime;

namespace PubSubHubbub.Models;

[Serializable]
public sealed class Entry
{
    [XmlElement("id", Namespace = Namespaces.Atom)]
    public required string Id { get; init; }

    [XmlElement("videoId", Namespace = Namespaces.YouTube)]
    public required string VideoId { get; init; }

    [XmlElement("channelId", Namespace = Namespaces.YouTube)]
    public required string ChannelId { get; init; }

    [XmlElement("title", Namespace = Namespaces.Atom)]
    public required string Title { get; init; }

    [XmlElement("link", Namespace = Namespaces.Atom)]
    public required Link Link { get; init; }

    [XmlElement("author", Namespace = Namespaces.Atom)]
    public required Author Author { get; init; }

    [XmlElement("published", Namespace = Namespaces.Atom)]
    public required OffsetDateTime Published { get; init; }

    [XmlElement("updated", Namespace = Namespaces.Atom)]
    public required OffsetDateTime Updated { get; init; }
}
