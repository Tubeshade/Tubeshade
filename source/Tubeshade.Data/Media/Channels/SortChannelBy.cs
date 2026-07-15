using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media.Channels;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class SortChannelBy : SmartEnum<SortChannelBy>, ISortBy, IParsable<SortChannelBy>
{
    public static readonly SortChannelBy Random = new(Names.Random, "random()", 1);
    public static readonly SortChannelBy CreatedAt = new(Names.CreatedAt, 2);
    public static readonly SortChannelBy ModifiedAt = new(Names.ModifiedAt, 3);
    public static readonly SortChannelBy VideoCount = new(Names.VideoCount, "video_count", 4);
    public static readonly SortChannelBy SubscriberCount = new(Names.SubscriberCount, 5);
    public static readonly SortChannelBy SubscribedAt = new(Names.SubscribedAt, 6);
    public static readonly SortChannelBy ChannelName = new(Names.ChannelName, 7);

    /// <inheritdoc />
    public string SortExpression { get; }

    private SortChannelBy(string name, int value)
        : this(name, $"channels.{name}", value)
    {
    }

    private SortChannelBy(string name, string sortExpression, int value)
        : base(name, value)
    {
        SortExpression = sortExpression;
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Random = "random";
        public const string CreatedAt = "created_at";
        public const string ModifiedAt = "modified_at";
        public const string VideoCount = "video_count";
        public const string SubscriberCount = "subscriber_count";
        public const string SubscribedAt = "subscribed_at";
        public const string ChannelName = "name";
    }

    /// <inheritdoc />
    public static SortChannelBy Parse(string s, IFormatProvider? provider)
    {
        return FromName(s, true);
    }

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out SortChannelBy result)
    {
        return TryFromName(s, true, out result);
    }
}
