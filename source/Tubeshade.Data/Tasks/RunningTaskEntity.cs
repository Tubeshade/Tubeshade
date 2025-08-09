using System;

namespace Tubeshade.Data.Tasks;

public sealed record RunningTaskEntity
{
    public required Guid Id { get; init; }
    public required TaskType Type { get; init; }
    public required string Payload { get; init; }

    public Guid? RunId { get; init; }
    public decimal? Value { get; init; }
    public decimal? Target { get; init; }

    public TaskResult? Result { get; init; }
    public string? Message { get; init; }
}
