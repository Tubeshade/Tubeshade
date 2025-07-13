using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Media;

public sealed class ImageType : SmartEnum<ImageType>
{
    public static readonly ImageType Thumbnail = new(Names.Thumbnail, 1);
    public static readonly ImageType Banner = new(Names.Banner, 2);

    private ImageType(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Thumbnail = "thumbnail";
        public const string Banner = "banner";
    }
}
