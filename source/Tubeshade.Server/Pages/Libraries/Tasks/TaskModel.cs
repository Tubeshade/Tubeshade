using System;

namespace Tubeshade.Server.Pages.Libraries.Tasks;

public sealed class TaskModel
{
    public required Guid Id { get; init; }

    public required TaskRunModel[] Runs { get; init; }
}
