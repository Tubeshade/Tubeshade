using System;
using System.IO;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Services;

public sealed class TaskContext<TService, TPayload>
{
    public required TService Service { get; init; }

    public required TPayload Payload { get; init; }

    public required TaskRepository TaskRepository { get; init; }

    public required Guid TaskRunId { get; init; }

    public required DirectoryInfo Directory { get; init; }
}
