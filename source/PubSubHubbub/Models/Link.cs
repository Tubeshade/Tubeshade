using System;
using System.Xml.Serialization;

namespace PubSubHubbub.Models;

[Serializable]
public sealed class Link
{
    [XmlAttribute("rel")]
    public required string Relation { get; init; }

    [XmlAttribute("href")]
    public required string Uri { get; init; }
}
