using System;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using NodaTime;
using NUnit.Framework;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Pages.Tasks;

namespace Tubeshade.Server.Tests.Pages.Tasks;

[SetCulture("")]
public sealed class TaskRunModelTests
{
    [TestCase(0, 10230000, 512, "0 MiB", "9.76 MiB", "512 B/s")]
    [TestCase(10230, 10230000, 10230, "0.01 MiB", "9.76 MiB", "9.99 KiB/s")]
    [TestCase(10230000, 10230000, 10230000, "9.76 MiB", "9.76 MiB", "9.76 MiB/s")]
    public void Download_ShouldFormatValueWithTargetUnit(
        decimal value,
        decimal target,
        decimal rate,
        string expectedValue,
        string expectedTarget,
        string expectedRate)
    {
        var run = CreateRun(TaskType.DownloadVideo, value, target, rate);

        using var scope = new AssertionScope();
        run.FormattedValue.Should().Be(expectedValue);
        run.FormattedTarget.Should().Be(expectedTarget);
        run.FormattedRate.Should().Be(expectedRate);
    }

    [Test]
    public void Download_WithoutTarget_ShouldFormatValueWithOwnUnit()
    {
        var run = CreateRun(TaskType.DownloadVideo, 10230, null, null);

        using var scope = new AssertionScope();
        run.FormattedValue.Should().Be("9.99 KiB");
        run.FormattedTarget.Should().BeNull();
        run.FormattedRate.Should().BeNull();
    }

    [TestCase(0, 2000, 0, "0", "2000", "0/s")]
    [TestCase(0, 2000, 0.001, "0", "2000", "1000 s")]
    [TestCase(0, 2000, 0.3, "0", "2000", "3.33 s")]
    [TestCase(0, 2000, 0.444, "0", "2000", "2.25 s")]
    [TestCase(0, 2000, 0.446, "0", "2000", "0.45/s")]
    [TestCase(0, 2000, 0.496, "0", "2000", "0.5/s")]
    [TestCase(0, 2000, 0.5, "0", "2000", "0.5/s")]
    [TestCase(1500, 2000, 12.346, "1500", "2000", "12.35/s")]
    public void Count_ShouldFormatAsNumbers(
        decimal value,
        decimal target,
        decimal rate,
        string expectedValue,
        string expectedTarget,
        string expectedRate)
    {
        var run = CreateRun(TaskType.ScanChannel, value, target, rate);

        using var scope = new AssertionScope();
        run.FormattedValue.Should().Be(expectedValue);
        run.FormattedTarget.Should().Be(expectedTarget);
        run.FormattedRate.Should().Be(expectedRate);
    }

    private static TaskRunModel CreateRun(TaskType type, decimal value, decimal? target, decimal? rate) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        State = RunState.Running,
        Source = TaskSource.User,
        CreatedAt = Instant.FromUnixTimeSeconds(0),
        Value = value,
        Target = target,
        Rate = rate
    };
}
