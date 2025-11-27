using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Media;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class SortVideoBy : SmartEnum<SortVideoBy>, IParsable<SortVideoBy>
{
    // Probably going to regret this at some point, but ORDER BY random() works for the current table sizes
    public static readonly SortVideoBy Random = new(Names.Random, "random()", 1);
    public static readonly SortVideoBy CreatedAt = new(Names.CreatedAt, 2);
    public static readonly SortVideoBy ModifiedAt = new(Names.ModifiedAt, 3);
    public static readonly SortVideoBy PublishedAt = new(Names.PublishedAt, 4);
    public static readonly SortVideoBy RefreshedAt = new(Names.RefreshedAt, 5);

    public string SortExpression { get; }

    private SortVideoBy(string name, int value)
        : this(name, $"videos.{name}", value)
    {
    }

    private SortVideoBy(string name, string sortExpression, int value)
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
        public const string PublishedAt = "published_at";
        public const string RefreshedAt = "refreshed_at";
    }

    /// <inheritdoc />
    public static SortVideoBy Parse(string s, IFormatProvider? provider)
    {
        return FromName(s, true);
    }

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out SortVideoBy result)
    {
        return TryFromName(s, true, out result);
    }
}
