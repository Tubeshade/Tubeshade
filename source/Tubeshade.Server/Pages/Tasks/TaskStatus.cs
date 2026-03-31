using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Pages.Tasks;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class TaskStatus : SmartEnum<TaskStatus>, IParsable<TaskStatus>
{
    public static readonly TaskStatus Queued = new(Names.Queued, 1);
    public static readonly TaskStatus InProgress = new(Names.InProgress, 2);
    public static readonly TaskStatus Completed = new(Names.Completed, 3);
    public static readonly TaskStatus Failed = new(Names.Failed, 4);
    public static readonly TaskStatus Cancelled = new(Names.Cancelled, 5);

    private TaskStatus(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Queued = "queued";
        public const string InProgress = "in_progress";
        public const string Completed = "completed";
        public const string Failed = "failed";
        public const string Cancelled = "cancelled";
    }

    public static TaskStatus FromResult(RunState state, TaskResult? result) => (state.Name, result?.Name) switch
    {
        (RunState.Names.Queued, null) => Queued,
        (RunState.Names.Running, null) => InProgress,
        (RunState.Names.Finished, { } resultName) => resultName switch
        {
            TaskResult.Names.Successful => Completed,
            TaskResult.Names.Failed => Failed,
            TaskResult.Names.Cancelled => Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, @"Unexpected result"),
        },
        _ => throw new ArgumentException($"Unexpected state {state.Name} and result {result?.Name} combination"),
    };

    public static (RunState? State, TaskResult? Result) ToResult(TaskStatus? status) => status?.Name switch
    {
        null => (null, null),
        Names.Queued => (RunState.Queued, null),
        Names.InProgress => (RunState.Running, null),
        Names.Completed => (RunState.Finished, TaskResult.Successful),
        Names.Failed => (RunState.Finished, TaskResult.Failed),
        Names.Cancelled => (RunState.Finished, TaskResult.Cancelled),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, @"Unexpected name"),
    };

    /// <inheritdoc />
    public static TaskStatus Parse(string s, IFormatProvider? provider)
    {
        return FromName(s, true);
    }

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out TaskStatus result)
    {
        return TryFromName(s, true, out result);
    }
}
