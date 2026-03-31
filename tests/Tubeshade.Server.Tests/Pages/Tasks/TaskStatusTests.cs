using FluentAssertions;
using NUnit.Framework;
using Tubeshade.Server.Pages.Tasks;

namespace Tubeshade.Server.Tests.Pages.Tasks;

public sealed class TaskStatusTests
{
    [TestCaseSource(typeof(TaskStatus), nameof(TaskStatus.List))]
    public void ToResult_ShouldRoundtrip(TaskStatus value)
    {
        var (state, result) = TaskStatus.ToResult(value);
        TaskStatus.FromResult(state!, result).Should().Be(value);
    }
}
