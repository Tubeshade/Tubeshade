using System;
using NodaTime;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Pages.Tasks;

public sealed class TaskRunModel
{
    private const decimal MiB = 1024 * 1024;

    public required Guid Id { get; init; }
    public required RunState State { get; init; }
    public required TaskSource Source { get; init; }
    public decimal? Value { get; init; }
    public decimal? Target { get; init; }
    public decimal? Rate { get; init; }
    public Period? Remaining { get; init; }
    public bool HasProgress => Status == TaskStatus.InProgress;
    public string? ProgressBarId => HasProgress ? $"progress-bar-{Id}" : null;

    public decimal Progress => Value is { } value && Target is { } target and not 0
        ? value / target
        : 0;

    public TaskResult? Result { get; init; }
    public string? Message { get; init; }

    public TaskStatus Status => TaskStatus.FromResult(State, Result);

    public string? FormattedValue => Value is { } value
        ? IsFileSize ? $"{Math.Round(value / MiB, 1)} MiB" : $"{value}"
        : null;

    public string? FormattedTarget => Target is { } target
        ? IsFileSize ? $"{Math.Round(target / MiB, 1)} MiB" : $"{target}"
        : null;

    public string? FormattedRate => Rate is { } rate
        ? IsFileSize ? $"{Math.Round(rate / MiB, 2)} MiB/s" : $"{Math.Round(rate, 2)}/s"
        : null;

    public string? FormattedRemaining => Remaining is not null
        ? HumanReadablePeriodPattern.Instance.Format(Remaining)
        : null;

    private bool IsFileSize => Target >= MiB;
}
