using System;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Pages.Libraries.Tasks;

public sealed class TaskRunModel
{
    public Guid Id { get; init; }
    public decimal? Value { get; init; }
    public decimal? Target { get; init; }
    public bool HasProgress => Status == TaskStatus.InProgress;
    public string? ProgressBarId => HasProgress ? $"progress-bar-{Id}" : null;
    public decimal Progress => Value / Target ?? 0;

    public TaskResult? Result { get; init; }
    public string? Message { get; init; }

    public TaskStatus Status => this switch
    {
        { Result: not null } => TaskStatus.FromResult(Result),
        _ => TaskStatus.InProgress,
    };
}
