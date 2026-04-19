using System;
using FluentAssertions;
using NodaTime;
using NUnit.Framework;
using Tubeshade.Server.Services.Background;

namespace Tubeshade.Server.Tests.Services;

[TestOf(typeof(SchedulerService))]
public sealed class SchedulerServiceTests
{
    private readonly SchedulerService _service = new(null!, null!, null!, null!, null!);

    [Test]
    public void GetNextTime()
    {
        const string cron = "*/15 * * * *";
        var currentTime = Instant.FromUtc(2025, 07, 19, 12, 00);

        var nextTime = _service.GetNextTime(cron, currentTime, Guid.NewGuid().GetHashCode());
        nextTime.Should().Be(Instant.FromUtc(2025, 07, 19, 12, 15));
    }

    [Test]
    public void GetNextTime_JitterShouldBeConsistent()
    {
        const string cron = "0 H * * *";
        var currentTime = Instant.FromUtc(2025, 07, 19, 12, 00);

        var nextTime = _service.GetNextTime(cron, currentTime, 0);
        nextTime.Should().Be(_service.GetNextTime(cron, currentTime, 0));
    }
}
