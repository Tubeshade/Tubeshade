using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Media.Videos;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class ViewStatus : SmartEnum<ViewStatus>, IParsable<ViewStatus>
{
    public static readonly ViewStatus Viewed = new(Names.Viewed, 1);
    public static readonly ViewStatus NotViewed = new(Names.NotViewed, 2);
    public static readonly ViewStatus PartiallyViewed = new(Names.PartiallyViewed, 3);

    private ViewStatus(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Viewed = "viewed";
        public const string NotViewed = "not_viewed";
        public const string PartiallyViewed = "partially_viewed";
    }

    /// <inheritdoc />
    public static ViewStatus Parse(string s, IFormatProvider? provider) => FromName(s, true);

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out ViewStatus result)
    {
        return TryFromName(s, true, out result);
    }
}
