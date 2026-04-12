using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Media;

public sealed class TrackType : SmartEnum<TrackType>, IParsable<TrackType>
{
    public static readonly TrackType Subtitles = new(Names.Subtitles, 1);
    public static readonly TrackType Chapters = new(Names.Chapters, 2);

    private TrackType(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Subtitles = "subtitles";
        public const string Chapters = "chapters";
    }

    /// <inheritdoc />
    public static TrackType Parse(string s, IFormatProvider? provider)
    {
        return FromName(s, true);
    }

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out TrackType result)
    {
        return TryFromName(s, true, out result);
    }
}
