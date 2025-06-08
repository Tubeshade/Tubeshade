using System;
using NodaTime;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Abstractions;

public abstract record ModifiableEntity : Entity, IModifiableEntity
{
    /// <inheritdoc />
    public Instant ModifiedAt { get; set; }

    /// <inheritdoc />
    public Guid ModifiedByUserId { get; set; }

    /// <inheritdoc />
    public Access AccessFoo => Access.Modify;
}
