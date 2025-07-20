using FluentAssertions;
using NodaTime;
using NUnit.Framework;
using Tubeshade.Server.Services.Background;

namespace Tubeshade.Server.Tests.Services;

[TestOf(typeof(SchedulerBackgroundService))]
public sealed class SchedulerBackgroundServiceTests
{
    [Test]
    public void GetNextTime()
    {
        const string cron = "*/15 * * * *";
        var currentTime = Instant.FromUtc(2025, 07, 19, 12, 00);

        var nextTime = SchedulerBackgroundService.GetNextTime(cron, currentTime);
        nextTime.Should().Be(Instant.FromUtc(2025, 07, 19, 12, 15));
    }
}
