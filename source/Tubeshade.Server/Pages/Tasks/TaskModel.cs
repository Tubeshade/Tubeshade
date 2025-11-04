using System;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Pages.Tasks;

public sealed class TaskModel
{
    public required Guid Id { get; init; }
    public required TaskType Type { get; init; }

    public required string? Name { get; init; }

    public required Guid? LibraryId { get; init; }

    public required Guid? ChannelId { get; init; }

    public required Guid? VideoId { get; init; }

    public required TaskRunModel[] Runs { get; init; }

    public required int TotalCount { get; init; }

    public string TypeDisplay => Type.Name switch
    {
        TaskType.Names.Index => "Index",
        TaskType.Names.DownloadVideo => "Download video",
        TaskType.Names.ScanChannel => "Scan channel",
        TaskType.Names.ScanSubscriptions => "Scan subscriptions",
        TaskType.Names.ScanSponsorBlockSegments => "Scan Sponsor Block segments",
        _ => throw new ArgumentOutOfRangeException(),
    };
}
