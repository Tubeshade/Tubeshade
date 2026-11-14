using System;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Tasks;

public sealed class TaskParameters : IAccessParameters, IPaginatedParameters
{
    public required long UserId { get; init; }

    /// <inheritdoc />
    public long PrincipalId => UserId;

    public Permissions Permissions => Permissions.Read;

    public Guid? LibraryId { get; init; }

    public TaskSource? Source { get; init; }

    public RunState? State { get; init; }

    public TaskResult? Result { get; init; }

    public Guid? TaskRunId { get; init; }

    public TaskType? Type { get; init; }

    public string? Url { get; init; }

    /// <inheritdoc />
    public required int Limit { get; init; }

    /// <inheritdoc />
    public required int Offset { get; init; }
}
