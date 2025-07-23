using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace SponsorBlock;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class SegmentCategory : SmartEnum<SegmentCategory, int>
{
    public static readonly SegmentCategory Sponsor = new(Names.Sponsor, 1);
    public static readonly SegmentCategory SelfPromotion = new(Names.SelfPromotion, 2);
    public static readonly SegmentCategory Interaction = new(Names.Interaction, 3);
    public static readonly SegmentCategory Intro = new(Names.Intro, 4);
    public static readonly SegmentCategory Outro = new(Names.Outro, 5);
    public static readonly SegmentCategory Preview = new(Names.Preview, 6);
    public static readonly SegmentCategory MusicOffTopic = new(Names.MusicOffTopic, 7);
    public static readonly SegmentCategory Filler = new(Names.Filler, 8);

    private SegmentCategory(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Sponsor = "sponsor";
        public const string SelfPromotion = "selfpromo";
        public const string Interaction = "interaction";
        public const string Intro = "intro";
        public const string Outro = "outro";
        public const string Preview = "preview";
        public const string MusicOffTopic = "music_offtopic";
        public const string Filler = "filler";
    }
}
