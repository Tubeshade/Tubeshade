using System;

namespace Tubeshade.Data.Tasks.Payloads;

public abstract class PayloadBase
{
    public required Guid UserId { get; init; }

    public required Guid LibraryId { get; init; }
}
