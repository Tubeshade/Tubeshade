using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media.Creators;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class SortCreatorBy : SmartEnum<SortCreatorBy>, ISortBy, IParsable<SortCreatorBy>
{
    public static readonly SortCreatorBy Random = new(Names.Random, "random()", 1);
    public static readonly SortCreatorBy CreatedAt = new(Names.CreatedAt, 2);
    public static readonly SortCreatorBy ModifiedAt = new(Names.ModifiedAt, 3);
    public static readonly SortCreatorBy ChannelCount = new(Names.ChannelCount, "channel_count", 4);
    public static readonly SortCreatorBy CreatorName = new(Names.CreatorName, 5);

    /// <inheritdoc />
    public string SortExpression { get; }

    private SortCreatorBy(string name, int value)
        : this(name, $"creators.{name}", value)
    {
    }

    private SortCreatorBy(string name, string sortExpression, int value)
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
        public const string ChannelCount = "channel_count";
        public const string CreatorName = "name";
    }

    /// <inheritdoc />
    public static SortCreatorBy Parse(string s, IFormatProvider? provider)
    {
        return FromName(s, true);
    }

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out SortCreatorBy result)
    {
        return TryFromName(s, true, out result);
    }
}
