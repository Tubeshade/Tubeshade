namespace Tubeshade.Data.Tasks;

public static class TaskChannels
{
    public const string Created = "task_created";
    public const string Cancel = "cancel_task";
    public const string RunFinished = "task_run_finished";

    public static string[] Names { get; } = [Created, Cancel, RunFinished];
}
