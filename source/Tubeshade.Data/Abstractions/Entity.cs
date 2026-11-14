using System;
using NodaTime;

namespace Tubeshade.Data.Abstractions;

public abstract record Entity : IEntity
{
    /// <inheritdoc />
    public long Id { get; set; }

    /// <inheritdoc />
    public Guid PublicId { get; set; }

    /// <inheritdoc />
    public Instant CreatedAt { get; init; }

    /// <inheritdoc />
    public long CreatedByUserId { get; init; }
}
