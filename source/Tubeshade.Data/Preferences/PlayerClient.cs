using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Preferences;

public sealed class PlayerClient : SmartEnum<PlayerClient>
{
    public static readonly PlayerClient Web = new(Names.Web, 1);
    public static readonly PlayerClient WebSafari = new(Names.WebSafari, 2);
    public static readonly PlayerClient MWeb = new(Names.MWeb, 3);
    public static readonly PlayerClient Tv = new(Names.Tv, 4);
    public static readonly PlayerClient TvSimply = new(Names.TvSimply, 5);
    public static readonly PlayerClient TvEmbedded = new(Names.TvEmbedded, 6);
    public static readonly PlayerClient WebEmbedded = new(Names.WebEmbedded, 7);
    public static readonly PlayerClient WebMusic = new(Names.WebMusic, 8);
    public static readonly PlayerClient WebCreator = new(Names.WebCreator, 9);
    public static readonly PlayerClient Android = new(Names.Android, 10);
    public static readonly PlayerClient AndroidVr = new(Names.AndroidVr, 11);
    public static readonly PlayerClient Ios = new(Names.Ios, 12);

    private PlayerClient(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Web = "web";
        public const string WebSafari = "web_safari";
        public const string MWeb = "mweb";
        public const string Tv = "tv";
        public const string TvSimply = "tv_simply";
        public const string TvEmbedded = "tv_embedded";
        public const string WebEmbedded = "web_embedded";
        public const string WebMusic = "web_music";
        public const string WebCreator = "web_creator";
        public const string Android = "android";
        public const string AndroidVr = "android_vr";
        public const string Ios = "ios";
    }
}
