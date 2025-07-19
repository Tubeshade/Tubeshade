using System;

namespace Tubeshade.Data.Tasks.Payloads;

public sealed class DownloadVideoPayload : PayloadBase
{
    public required Guid VideoId { get; init; }
}
