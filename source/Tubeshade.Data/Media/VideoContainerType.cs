using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Media;

public sealed class VideoContainerType : SmartEnum<VideoContainerType>
{
    public static readonly VideoContainerType Mp4 = new(Names.Mp4, 1);
    public static readonly VideoContainerType WebM = new(Names.WebM, 2);

    private VideoContainerType(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Mp4 = "mp4";
        public const string WebM = "webm";
    }
}
