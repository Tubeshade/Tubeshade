using System;

namespace Tubeshade.Data.Tasks.Payloads;

public sealed class IndexPayload
{
    public required Guid UserId { get; init; }
    public required Guid LibraryId { get; init; }
    public required string Url { get; init; }
}
