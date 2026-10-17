using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Services.Background;

internal readonly record struct DequeueResult
{
    public bool TaskLocked { get; }

    public DequeuedTask? Task { get; }

    private DequeueResult(bool taskLocked, DequeuedTask? task)
    {
        TaskLocked = taskLocked;
        Task = task;
    }

    internal static DequeueResult Nothing => new(false, null);

    internal static DequeueResult Locked => new(true, null);

    internal static DequeueResult Dequeued(TaskEntity task, TaskSource source) => new(false, new(task, source));

    internal readonly record struct DequeuedTask(TaskEntity Task, TaskSource Source);
}
