using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Preferences;

public sealed class DownloadMethod : SmartEnum<DownloadMethod>, IParsable<DownloadMethod>
{
    public static readonly DownloadMethod Default = new(Names.Default, 1);
    public static readonly DownloadMethod Streaming = new(Names.Streaming, 2);

    private DownloadMethod(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Default = "standard";
        public const string Streaming = "streaming";
    }

    /// <inheritdoc />
    public static DownloadMethod Parse(string s, IFormatProvider? provider)
    {
        return FromName(s, true);
    }

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out DownloadMethod result)
    {
        return TryFromName(s, true, out result);
    }
}
