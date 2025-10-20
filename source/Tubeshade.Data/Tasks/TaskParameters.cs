using System;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Tasks;

public sealed class TaskParameters : IAccessParameters, IPaginatedParameters
{
    public required Guid UserId { get; init; }

    /// <inheritdoc />
    public Access Access => Access.Read;

    public Guid? LibraryId { get; init; }

    /// <inheritdoc />
    public required int Limit { get; init; }

    /// <inheritdoc />
    public required int Offset { get; init; }
}
