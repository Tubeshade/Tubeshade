using System;
using NodaTime;

namespace Tubeshade.Server.V1.Models;

public sealed class Library
{
    public required Guid Id { get; init; }

    public required Instant CreatedAt { get; init; }

    public required Instant ModifiedAt { get; init; }

    public required string Name { get; init; }
}
