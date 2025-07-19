using System;

namespace Tubeshade.Data.Tasks.Payloads;

public sealed class ScanChannelPayload : PayloadBase
{
    public required Guid ChannelId { get; init; }
}
