using System;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Services.Background;

public readonly struct CreatedTask
{
    public required Guid Id { get; init; }

    public required TaskSource Source { get; init; }
}
