using System;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Tasks;

public sealed record TaskEntity : ModifiableEntity, IOwnableEntity
{
    /// <inheritdoc />
    public required Guid OwnerId { get; set; }

    public required TaskType Type { get; init; }

    public Guid? UserId { get; init; }

    public Guid? LibraryId { get; set; }

    public Guid? ChannelId { get; set; }

    public Guid? VideoId { get; set; }

    public string? Url { get; init; }

    public bool AllVideos { get; init; }
}
