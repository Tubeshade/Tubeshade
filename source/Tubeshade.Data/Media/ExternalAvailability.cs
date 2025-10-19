using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Media;

public sealed class ExternalAvailability : SmartEnum<ExternalAvailability>, IParsable<ExternalAvailability>
{
    public static readonly ExternalAvailability Public = new(Names.Public, 1);
    public static readonly ExternalAvailability Private = new(Names.Private, 2);
    public static readonly ExternalAvailability NotAvailable = new(Names.NotAvailable, 3);

    private ExternalAvailability(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Public = "public";
        public const string Private = "private";
        public const string NotAvailable = "not_available";
    }

    /// <inheritdoc />
    public static ExternalAvailability Parse(string s, IFormatProvider? provider)
    {
        return FromName(s, true);
    }

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out ExternalAvailability result)
    {
        return TryFromName(s, true, out result);
    }
}
