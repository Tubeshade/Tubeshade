using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Media;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class SortDirection : SmartEnum<SortDirection>, IParsable<SortDirection>
{
    public static readonly SortDirection Ascending = new(Names.Ascending, 1);
    public static readonly SortDirection Descending = new(Names.Descending, 2);

    private SortDirection(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Ascending = "ASC";
        public const string Descending = "DESC";
    }

    /// <inheritdoc />
    public static SortDirection Parse(string s, IFormatProvider? provider)
    {
        return FromName(s, true);
    }

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out SortDirection result)
    {
        return TryFromName(s, true, out result);
    }
}
