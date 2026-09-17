using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media.Playlists;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class SortPlaylistBy : SmartEnum<SortPlaylistBy>, ISortBy, IParsable<SortPlaylistBy>
{
    public static readonly SortPlaylistBy Random = new(Names.Random, "random()", 1);
    public static readonly SortPlaylistBy CreatedAt = new(Names.CreatedAt, 2);
    public static readonly SortPlaylistBy ModifiedAt = new(Names.ModifiedAt, 3);
    public static readonly SortPlaylistBy SubscribedAt = new(Names.SubscribedAt, 4);
    public static readonly SortPlaylistBy PlaylistName = new(Names.PlaylistName, 5);
    public static readonly SortPlaylistBy RefreshedAt = new(Names.RefreshedAt, 6);
    public static readonly SortPlaylistBy VideoCount = new(Names.VideoCount, "video_count", 7);

    /// <inheritdoc />
    public string SortExpression { get; }

    private SortPlaylistBy(string name, int value)
        : this(name, $"playlists.{name}", value)
    {
    }

    private SortPlaylistBy(string name, string sortExpression, int value)
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
        public const string SubscribedAt = "subscribed_at";
        public const string PlaylistName = "name";
        public const string RefreshedAt = "refreshed_at";
        public const string VideoCount = "video_count";
    }

    /// <inheritdoc />
    public static SortPlaylistBy Parse(string s, IFormatProvider? provider)
    {
        return FromName(s, true);
    }

    /// <inheritdoc />
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out SortPlaylistBy result)
    {
        return TryFromName(s, true, out result);
    }
}
