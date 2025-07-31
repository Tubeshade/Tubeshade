using System;

namespace Tubeshade.Data.Abstractions;

public sealed class EntityId
{
    public required Guid Id { get; init; }

    public required string ExternalId { get; init; }
}
