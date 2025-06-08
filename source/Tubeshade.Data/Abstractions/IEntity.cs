using System;
using NodaTime;

namespace Tubeshade.Data.Abstractions;

public interface IEntity
{
    Guid Id { get; set; }

    Instant CreatedAt { get; init; }

    Guid CreatedByUserId { get; init; }
}
