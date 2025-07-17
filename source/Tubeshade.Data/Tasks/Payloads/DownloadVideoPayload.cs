using System;

namespace Tubeshade.Data.Tasks.Payloads;

public sealed class DownloadVideoPayload
{
    public required Guid UserId { get; init; }
    public required Guid LibraryId { get; init; }
    public required Guid VideoId { get; init; }
}
