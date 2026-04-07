using System;
using System.Xml.Serialization;
using NodaTime;

namespace PubSubHubbub.Models;

[Serializable]
[XmlRoot("feed", Namespace = Namespaces.Atom)]
public sealed class Feed
{
    [XmlElement("link", Namespace = Namespaces.Atom)]
    public Link[]? Links { get; init; }

    [XmlElement("title", Namespace = Namespaces.Atom)]
    public string? Title { get; init; }

    [XmlElement("updated", Namespace = Namespaces.Atom)]
    public OffsetDateTime? Updated { get; init; }

    [XmlIgnore]
    public bool UpdatedSpecified => Updated is not null;

    [XmlElement("entry", Namespace = Namespaces.Atom)]
    public Entry? Entry { get; init; }

    [XmlElement("deleted-entry", Namespace = Namespaces.Tombstones)]
    public DeletedEntry? DeletedEntry { get; init; }
}
