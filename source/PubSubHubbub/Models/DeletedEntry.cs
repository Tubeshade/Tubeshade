using System;
using System.Xml.Serialization;

namespace PubSubHubbub.Models;

[Serializable]
public sealed class DeletedEntry
{
    [XmlAttribute("ref")]
    public required string VideoId { get; init; }

    // Cannot use user-defined types for XmlAttribute, if needed create a separate property of type OffsetDateTime that is ignored
    [XmlAttribute("when")]
    public required string When { get; init; }

    [XmlElement("link", Namespace = Namespaces.Atom)]
    public required Link Link { get; init; }

    [XmlElement("by", Namespace = Namespaces.Tombstones)]
    public required Author By { get; init; }
}
