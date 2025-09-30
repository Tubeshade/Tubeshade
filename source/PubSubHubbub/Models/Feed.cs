using System;
using System.Xml.Serialization;
using NodaTime;

namespace PubSubHubbub.Models;

[Serializable]
[XmlRoot("feed", Namespace = "http://www.w3.org/2005/Atom")]
public sealed class Feed
{
    [XmlElement("link")]
    public required Link[] Links { get; init; }

    [XmlElement("title")]
    public required string Title { get; init; }

    [XmlElement("updated")]
    public required OffsetDateTime Updated { get; init; }

    [XmlElement("entry")]
    public required Entry Entry { get; init; }
}
