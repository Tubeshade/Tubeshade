using System;
using NodaTime;
using NodaTime.Text;

namespace Tubeshade.Server.Configuration;

public sealed class SchedulerOptions
{
    internal static readonly PeriodPattern Pattern = PeriodPattern.NormalizingIso;

    public const string SectionName = "Scheduler";

    public required string Period { get; set; } = "PT1M";

    internal Period GetPeriod() => Pattern.Parse(Period).Value;

    internal TimeSpan GetPeriodTimeSpan() => GetPeriod().ToDuration().ToTimeSpan();
}
