using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Tasks;

public sealed class TaskSource : SmartEnum<TaskSource>, IParsable<TaskSource>
{
    public static readonly TaskSource Unknown = new(Names.Unknown, 1);
    public static readonly TaskSource User = new(Names.User, 2);
    public static readonly TaskSource Schedule = new(Names.Schedule, 3);
    public static readonly TaskSource Webhook = new(Names.Webhook, 4);

    private TaskSource(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Unknown = "unknown";
        public const string User = "user";
        public const string Schedule = "schedule";
        public const string Webhook = "webhook";
    }

    /// <inheritdoc />
    public static TaskSource Parse(string s, IFormatProvider? provider)
    {
        return FromName(s, true);
    }

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out TaskSource result)
    {
        return TryFromName(s, true, out result);
    }
}
