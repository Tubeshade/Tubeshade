using System;
using NodaTime;

namespace Tubeshade.Data.Abstractions;

public abstract record Entity : IEntity
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public Instant CreatedAt { get; init; }

    /// <inheritdoc />
    public Guid CreatedByUserId { get; init; }
}
