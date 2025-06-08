using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Tasks;

public sealed class TaskResult : SmartEnum<TaskResult>
{
    public static readonly TaskResult Successful = new(Names.Successful, 1);
    public static readonly TaskResult Failed = new(Names.Failed, 2);
    public static readonly TaskResult Cancelled = new(Names.Cancelled, 3);

    private TaskResult(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Successful = "successful";
        public const string Failed = "failed";
        public const string Cancelled = "cancelled";
    }
}
