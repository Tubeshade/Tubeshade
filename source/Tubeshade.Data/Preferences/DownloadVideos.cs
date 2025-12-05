using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Preferences;

public sealed class DownloadVideos : SmartEnum<DownloadVideos>, IParsable<DownloadVideos>
{
    public static readonly DownloadVideos All = new(Names.All, 1, true);
    public static readonly DownloadVideos New = new(Names.New, 2, true);
    public static readonly DownloadVideos None = new(Names.None, 3, false);

    public bool Download { get; }

    private DownloadVideos(string name, int value, bool download)
        : base(name, value)
    {
        Download = download;
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string All = "all";
        public const string New = "new";
        public const string None = "none";
    }

    /// <inheritdoc />
    public static DownloadVideos Parse(string s, IFormatProvider? provider)
    {
        return FromName(s, true);
    }

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out DownloadVideos result)
    {
        return TryFromName(s, true, out result);
    }
}
