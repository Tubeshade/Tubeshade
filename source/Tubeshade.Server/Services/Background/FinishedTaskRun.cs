using System;
using System.Globalization;

namespace Tubeshade.Server.Services.Background;

/// <summary>The payload of <c>task_run_finished</c>: <c>{id}:{public_id}</c>, see <c>TaskRepository.FinishTaskRun.sql</c>.</summary>
/// <param name="Id">The internal id, which blocking task runs are matched by.</param>
/// <param name="PublicId">The public id, for logging.</param>
public readonly record struct FinishedTaskRun(long Id, Guid PublicId)
{
    internal static bool TryParse(string payload, out FinishedTaskRun run)
    {
        run = default;

        var separator = payload.IndexOf(':');
        if (separator < 0 ||
            !long.TryParse(payload.AsSpan(0, separator), NumberStyles.None, CultureInfo.InvariantCulture, out var id) ||
            !Guid.TryParseExact(payload.AsSpan(separator + 1), "D", out var publicId))
        {
            return false;
        }

        run = new FinishedTaskRun(id, publicId);
        return true;
    }
}
