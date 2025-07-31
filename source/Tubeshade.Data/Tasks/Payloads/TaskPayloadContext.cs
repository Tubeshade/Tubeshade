using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tubeshade.Data.Tasks.Payloads;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(IndexPayload))]
[JsonSerializable(typeof(DownloadVideoPayload))]
[JsonSerializable(typeof(ScanChannelPayload))]
[JsonSerializable(typeof(ScanSubscriptionsPayload))]
[JsonSerializable(typeof(ScanSponsorBlockSegmentsPayload))]
public sealed partial class TaskPayloadContext : JsonSerializerContext;

public interface ITaskPayload
{
    public static abstract TaskType TaskType { get; }
}

public abstract class PayloadBase
{
    public required Guid UserId { get; init; }

    public required Guid LibraryId { get; init; }
}

public sealed class DownloadVideoPayload : PayloadBase, ITaskPayload
{
    /// <inheritdoc />
    public static TaskType TaskType => TaskType.DownloadVideo;

    public required Guid VideoId { get; init; }
}

public sealed class IndexPayload : PayloadBase, ITaskPayload
{
    /// <inheritdoc />
    public static TaskType TaskType => TaskType.Index;

    public required string Url { get; init; }
}

public sealed class ScanChannelPayload : PayloadBase, ITaskPayload
{
    /// <inheritdoc />
    public static TaskType TaskType => TaskType.ScanChannel;

    public required Guid ChannelId { get; init; }

    public bool All { get; init; }
}

public sealed class ScanSubscriptionsPayload : PayloadBase, ITaskPayload
{
    /// <inheritdoc />
    public static TaskType TaskType => TaskType.ScanSubscriptions;
}

public sealed class ScanSponsorBlockSegmentsPayload : PayloadBase, ITaskPayload
{
    /// <inheritdoc />
    public static TaskType TaskType => TaskType.ScanSponsorBlockSegments;
}
