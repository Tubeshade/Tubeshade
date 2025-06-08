using System;
using NodaTime;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Abstractions;

/// <summary>Represents an entity that can be updated.</summary>
public interface IModifiableEntity : IEntity
{
    Instant ModifiedAt { get; set; }

    Guid ModifiedByUserId { get; set; }

    Access AccessFoo { get; }
}
