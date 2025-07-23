using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace SponsorBlock;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class SegmentService : SmartEnum<SegmentService, int>
{
    public static readonly SegmentService YouTube = new(Names.YouTube, 1);

    private SegmentService(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string YouTube = "YouTube";
    }
}
