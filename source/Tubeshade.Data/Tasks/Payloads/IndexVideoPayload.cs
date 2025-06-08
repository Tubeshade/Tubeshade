using System;

namespace Tubeshade.Data.Tasks.Payloads;

public sealed class IndexVideoPayload
{
    public required Guid UserId { get; init; }
    public required Guid LibraryId { get; init; }
    public required string VideoUrl { get; init; }
}

public sealed class DownloadVideoPayload
{
    public required Guid UserId { get; init; }
    public required Guid LibraryId { get; init; }
    public required Guid VideoId { get; init; }
}
