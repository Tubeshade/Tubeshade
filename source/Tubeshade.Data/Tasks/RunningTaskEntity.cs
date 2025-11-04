using System;
using NodaTime;

namespace Tubeshade.Data.Tasks;

public sealed record RunningTaskEntity
{
    public required Guid Id { get; init; }
    public required TaskType Type { get; init; }

    public Guid? LibraryId { get; init; }
    public Guid? ChannelId { get; init; }
    public Guid? VideoId { get; init; }

    public Guid RunId { get; init; }
    public decimal? Value { get; init; }
    public decimal? Target { get; init; }
    public decimal? Rate { get; init; }
    public Period? RemainingDuration { get; init; }

    public TaskResult? Result { get; init; }
    public string? Message { get; init; }
    public string? Name { get; init; }

    public required int TotalCount { get; init; }
}
