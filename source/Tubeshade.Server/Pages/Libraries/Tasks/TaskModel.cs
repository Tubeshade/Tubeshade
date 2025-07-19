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
        TaskType.Names.Index => "Index",
        TaskType.Names.DownloadVideo => "Download video",
        TaskType.Names.ScanChannel => "Scan channel",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public string Name => Type.Name switch
    {
        TaskType.Names.Index => JsonSerializer.Deserialize(Payload, TaskPayloadContext.Default.IndexPayload)!.Url,
        TaskType.Names.DownloadVideo => JsonSerializer.Deserialize(Payload, TaskPayloadContext.Default.DownloadVideoPayload)!.VideoId.ToString(),
        TaskType.Names.ScanChannel => JsonSerializer.Deserialize(Payload, TaskPayloadContext.Default.ScanChannelPayload)!.ChannelId.ToString(),
        _ => throw new ArgumentOutOfRangeException(),
    };

    public TaskStatus Status => this switch
    {
        { Result: not null } => TaskStatus.FromResult(Result),
        { RunId: not null } => TaskStatus.InProgress,
        _ => TaskStatus.Queued,
    };
}
