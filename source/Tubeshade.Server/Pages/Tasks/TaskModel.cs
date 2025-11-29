using System;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Resources;

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

    public string? TypeDisplay => SharedResources.ResourceManager.GetString($"Tasks_Type_{Type.Name}") ?? Type.Name;
}
