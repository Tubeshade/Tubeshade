using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Pages.Tasks;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class TaskStatus : SmartEnum<TaskStatus>
{
    public static readonly TaskStatus Queued = new("queued", 1);
    public static readonly TaskStatus InProgress = new("in_progress", 2);
    public static readonly TaskStatus Completed = new("completed", 3);
    public static readonly TaskStatus Failed = new("failed", 4);
    public static readonly TaskStatus Cancelled = new("cancelled", 5);

    private TaskStatus(string name, int value)
        : base(name, value)
    {
    }

    public static TaskStatus FromResult(RunState state, TaskResult? result) => (state.Name, result?.Name) switch
    {
        (RunState.Names.Queued, null) => Queued,
        (RunState.Names.Running, null) => InProgress,
        (RunState.Names.Finished, { } resultName) => resultName switch
        {
            TaskResult.Names.Successful => Completed,
            TaskResult.Names.Failed => Failed,
            TaskResult.Names.Cancelled => Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, "Unexpected result"),
        },
        _ => throw new ArgumentException($"Unexpected state {state.Name} and result {result?.Name} combination"),
    };
}
