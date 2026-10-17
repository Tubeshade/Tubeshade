using System;

namespace Tubeshade.Data.Tasks;

public sealed class TaskRequest
{
    public required Guid TaskId { get; init; }

    public required TaskSource Source { get; init; }
}
