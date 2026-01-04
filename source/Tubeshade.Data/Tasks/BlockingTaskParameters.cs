using System;

namespace Tubeshade.Data.Tasks;

public sealed class BlockingTaskParameters
{
    public required string? Url { get; init; }

    public required Guid? VideoId { get; init; }

    public required Guid? ChannelId { get; init; }

    public required TaskType Type { get; init; }

    public required Guid RunId { get; init; }

    public static BlockingTaskParameters FromTask(TaskEntity task, Guid taskRunId) => new()
    {
        Url = task.Url,
        VideoId = task.VideoId,
        ChannelId = task.ChannelId,
        Type = task.Type,
        RunId = taskRunId,
    };
}
