using System;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Tasks;

public sealed record ScheduleEntity : ModifiableEntity
{
    public required Guid TaskId { get; set; }

    public required string CronExpression { get; set; }

    public required string TimeZoneId { get; set; }
}
