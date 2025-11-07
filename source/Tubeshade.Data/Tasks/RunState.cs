using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Tasks;

public sealed class RunState : SmartEnum<RunState>
{
    public static readonly RunState Queued = new(Names.Queued, 1);
    public static readonly RunState Running = new(Names.Running, 2);
    public static readonly RunState Finished = new(Names.Finished, 3);

    private RunState(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Queued = "queued";
        public const string Running = "running";
        public const string Finished = "finished";
    }
}
