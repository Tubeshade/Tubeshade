using System;
using System.Xml.Serialization;

namespace PubSubHubbub.Models;

[Serializable]
public sealed class Author
{
    [XmlElement("name", Namespace = Namespaces.Atom)]
    public required string Name { get; init; }

    [XmlElement("uri", Namespace = Namespaces.Atom)]
    public required string Uri { get; init; }
}
