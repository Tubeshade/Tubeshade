using System;

namespace Tubeshade.Data.Media.Creators;

public sealed class CreatorChannel
{
    public Guid ChannelId { get; init; }

    public bool Primary { get; init; }
}
