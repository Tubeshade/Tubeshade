using System;
using System.Text.Json;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;

namespace Tubeshade.Server.Pages.Libraries.Tasks;

public sealed class TaskModel
{
    public required Guid Id { get; init; }
    public required TaskType Type { get; init; }
    public required string Payload { get; init; }

    public Guid? RunId { get; init; }
    public decimal? Value { get; init; }
    public decimal? Target { get; init; }
    public bool HasProgress => Status == TaskStatus.InProgress;
    public string? ProgressBarId => HasProgress ? $"progress-bar-{Id}" : null;
    public decimal Progress => Value / Target ?? 0;

    public TaskResult? Result { get; init; }
    public string? Message { get; init; }

    public string TypeDisplay => Type.Name switch
    {
        "index_video" => "Index video",
        "download_video" => "Download video",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public string Name => Type.Name switch
    {
        "index_video" => JsonSerializer.Deserialize(Payload, TaskPayloadContext.Default.IndexVideoPayload)!.VideoUrl,
        "download_video" => JsonSerializer.Deserialize(Payload, TaskPayloadContext.Default.DownloadVideoPayload)!.VideoId.ToString(),
        _ => throw new ArgumentOutOfRangeException(),
    };

    public TaskStatus Status => this switch
    {
        { Result: not null } => TaskStatus.FromResult(Result),
        { RunId: not null } => TaskStatus.InProgress,
        _ => TaskStatus.Queued,
    };
}
