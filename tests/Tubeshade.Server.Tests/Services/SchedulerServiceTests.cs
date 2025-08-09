using FluentAssertions;
using NodaTime;
using NUnit.Framework;
using Tubeshade.Server.Services.Background;

namespace Tubeshade.Server.Tests.Services;

[TestOf(typeof(SchedulerService))]
public sealed class SchedulerServiceTests
{
    [Test]
    public void GetNextTime()
    {
        const string cron = "*/15 * * * *";
        var currentTime = Instant.FromUtc(2025, 07, 19, 12, 00);

        var nextTime = SchedulerService.GetNextTime(cron, currentTime);
        nextTime.Should().Be(Instant.FromUtc(2025, 07, 19, 12, 15));
    }
}
