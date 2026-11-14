using System;

namespace Tubeshade.Data.Tasks;

public sealed class TaskRunRequest
{
    public required long RunId { get; init; }

    public required Guid TaskId { get; init; }

    public required TaskSource Source { get; init; }
}
