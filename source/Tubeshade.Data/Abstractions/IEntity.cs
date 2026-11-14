using System;
using NodaTime;

namespace Tubeshade.Data.Abstractions;

public interface IEntity
{
    long Id { get; set; }

    Guid PublicId { get; set; }

    Instant CreatedAt { get; init; }

    long CreatedByUserId { get; init; }
}
