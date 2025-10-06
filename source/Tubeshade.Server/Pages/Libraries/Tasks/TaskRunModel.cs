using System;
using NodaTime;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Pages.Libraries.Tasks;

public sealed class TaskRunModel
{
    private const decimal MiB = 1024 * 1024;

    public Guid Id { get; init; }
    public decimal? Value { get; init; }
    public decimal? Target { get; init; }
    public decimal? Rate { get; init; }
    public Period? Remaining { get; init; }
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

    public string? FormattedValue => Value is { } value
        ? IsFileSize ? $"{Math.Round(value / MiB, 1)} MiB" : $"{value}"
        : null;

    public string? FormattedTarget => Target is { } target
        ? IsFileSize ? $"{Math.Round(target / MiB, 1)} MiB" : $"{target}"
        : null;

    public string? FormattedRate => Rate is { } rate
        ? $"{Math.Round(rate / MiB, 2)} MiB/s"
        : null;

    public string? FormattedRemaining => Remaining is not null
        ? HumanReadablePeriodPattern.Instance.Format(Remaining)
        : null;

    private bool IsFileSize => Target is >= 10 * MiB;
}
