using System;

namespace Tubeshade.Server.Services;

public sealed class UrlIndexingResult
{
    public required Guid ChannelId { get; init; }

    public Guid? VideoId { get; set; }
}
