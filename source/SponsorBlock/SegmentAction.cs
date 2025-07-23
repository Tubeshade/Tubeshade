using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace SponsorBlock;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class SegmentAction : SmartEnum<SegmentAction, int>
{
    public static readonly SegmentAction Skip = new(Names.Skip, 1);
    public static readonly SegmentAction Mute = new(Names.Mute, 2);
    public static readonly SegmentAction Full = new(Names.Full, 3);
    public static readonly SegmentAction PointOfInterest = new(Names.PointOfInterest, 4);
    public static readonly SegmentAction Chapter = new(Names.Chapter, 5);

    private SegmentAction(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Skip = "skip";
        public const string Mute = "mute";
        public const string Full = "full";
        public const string PointOfInterest = "poi";
        public const string Chapter = "chapter";
    }
}
