using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class UpdateScheduleModel
{
    public UpdateScheduleModel()
    {
    }

    public UpdateScheduleModel(ScheduleEntity schedule)
    {
        Id = schedule.Id;
        CronExpression = schedule.CronExpression;
        TimeZoneId = schedule.TimeZoneId;
    }

    [Required]
    public Guid? Id { get; set; }

    [Required]
    [CronExpression]
    public string CronExpression { get; set; } = null!;

    [Required]
    [TimeZone]
    public string TimeZoneId { get; set; } = null!;

    [Browsable(false)]
    internal IEnumerable<string> TimeZoneIds { get; set; } = [];
}
