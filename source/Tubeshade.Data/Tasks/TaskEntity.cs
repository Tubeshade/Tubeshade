using System;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Tasks;

public sealed record TaskEntity : ModifiableEntity, IOwnableEntity
{
    /// <inheritdoc />
    public required Guid OwnerId { get; set; }

    public required TaskType Type { get; set; }

    public required string Payload { get; set; }
}
