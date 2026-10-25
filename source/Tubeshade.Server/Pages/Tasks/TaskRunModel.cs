using System;
using System.Globalization;
using NodaTime;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Pages.Tasks;

public sealed class TaskRunModel
{
    public required Guid Id { get; init; }
    public required TaskType Type { get; init; }
    public required RunState State { get; init; }
    public required TaskSource Source { get; init; }
    public required Instant CreatedAt { get; init; }
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
        ? Format(value, Target ?? value)
        : null;

    public string? FormattedTarget => Target is { } target
        ? Format(target, target)
        : null;

    public string? FormattedRate => Rate is { } rate
        ? $"{Format(rate, rate)}/s"
        : null;

    public string? FormattedRemaining => Remaining is not null
        ? HumanReadablePeriodPattern.Instance.Format(Remaining)
        : null;

    private string Format(decimal value, decimal reference) => Type == TaskType.DownloadVideo
        ? value.FormatSize(reference, CultureInfo.CurrentCulture)
        : Math.Round(value, 2).ToString(CultureInfo.CurrentCulture);
}
