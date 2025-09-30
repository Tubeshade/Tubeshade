using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Media;

public sealed class VideoType : SmartEnum<VideoType>
{
    public static readonly VideoType Video = new(Names.Video, 1, "videos");
    public static readonly VideoType Short = new(Names.Short, 2, "shorts");
    public static readonly VideoType Livestream = new(Names.Livestream, 3, "streams");

    public string Tab { get; }

    private VideoType(string name, int value, string tab)
        : base(name, value)
    {
        Tab = tab;
    }

    public static bool TryFromUrl(string url, [NotNullWhen(true)] out VideoType? videoType)
    {
        if (url.Contains("/shorts/"))
        {
            videoType = Short;
            return true;
        }

        if (url.Contains("/watch"))
        {
            videoType = Video;
            return true;
        }

        videoType = null;
        return false;
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Video = "video";
        public const string Short = "short";
        public const string Livestream = "livestream";
    }
}
