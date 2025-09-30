using System;
using System.Xml.Serialization;

namespace PubSubHubbub.Models;

[Serializable]
public sealed class Author
{
    [XmlElement("name")]
    public required string Name { get; init; }

    [XmlElement("uri")]
    public required string Uri { get; init; }
}
