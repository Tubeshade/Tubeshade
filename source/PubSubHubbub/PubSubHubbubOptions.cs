using System;
using System.ComponentModel.DataAnnotations;

namespace PubSubHubbub;

public sealed class PubSubHubbubOptions
{
    public const string SectionName = "PubSubHubbub";

    [Required]
    public Uri BaseUrl { get; set; } = new("https://pubsubhubbub.appspot.com", UriKind.Absolute);

    public Uri? CallbackBaseUri { get; set; }

    [MaxLength(200)]
    public string? Secret { get; set; }
}
