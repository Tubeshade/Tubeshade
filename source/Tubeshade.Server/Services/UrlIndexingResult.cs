using System;

namespace Tubeshade.Server.Services;

public sealed class UrlIndexingResult
{
    public Guid? ChannelId { get; set; }

    public Guid? VideoId { get; set; }

    public Guid? PlaylistId { get; set; }
}
