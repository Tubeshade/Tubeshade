using System;
using Ardalis.SmartEnum;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Pages.Libraries.Tasks;

public sealed class TaskStatus : SmartEnum<TaskStatus>
{
    public static readonly TaskStatus Queued = new("Queued", 1);
    public static readonly TaskStatus InProgress = new("In progress", 2);
    public static readonly TaskStatus Completed = new("Completed", 3);
    public static readonly TaskStatus Failed = new("Failed", 4);
    public static readonly TaskStatus Cancelled = new("Cancelled", 5);

    private TaskStatus(string name, int value)
        : base(name, value)
    {
    }

    public static TaskStatus FromResult(TaskResult result) => result.Name switch
    {
        TaskResult.Names.Successful => Completed,
        TaskResult.Names.Failed => Failed,
        TaskResult.Names.Cancelled => Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, "Unexpected result"),
    };
}
