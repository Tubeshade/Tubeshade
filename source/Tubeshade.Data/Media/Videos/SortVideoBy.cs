using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media.Videos;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class SortVideoBy : SmartEnum<SortVideoBy>, ISortBy, IParsable<SortVideoBy>
{
    // Probably going to regret this at some point, but ORDER BY random() works for the current table sizes
    public static readonly SortVideoBy Random = new(Names.Random, 1);
    public static readonly SortVideoBy CreatedAt = new(Names.CreatedAt, 2);
    public static readonly SortVideoBy ModifiedAt = new(Names.ModifiedAt, 3);
    public static readonly SortVideoBy PublishedAt = new(Names.PublishedAt, 4);
    public static readonly SortVideoBy RefreshedAt = new(Names.RefreshedAt, 5);
    public static readonly SortVideoBy ViewCount = new(Names.ViewCount, 6);
    public static readonly SortVideoBy LikeCount = new(Names.LikeCount, 7);
    public static readonly SortVideoBy DownloadedAt = new(Names.DownloadedAt, 8);
    public static readonly SortVideoBy Duration = new(Names.Duration, 9);
    public static readonly SortVideoBy StorageSize = new(Names.StorageSize, 10);
    public static readonly SortVideoBy Framerate = new(Names.Framerate, 11);
    public static readonly SortVideoBy Query = new(Names.Query, 12);

    /// <summary>Only meaningful when the videos are filtered by <see cref="VideoParameters.PlaylistId"/>.</summary>
    public static readonly SortVideoBy PlaylistOrder = new(Names.PlaylistOrder, 13);

    private SortVideoBy(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Random = "random";
        public const string CreatedAt = "created_at";
        public const string ModifiedAt = "modified_at";
        public const string PublishedAt = "published_at";
        public const string RefreshedAt = "refreshed_at";
        public const string ViewCount = "view_count";
        public const string LikeCount = "like_count";
        public const string DownloadedAt = "downloaded_at";
        public const string Duration = "duration";
        public const string StorageSize = "storage_size";
        public const string Framerate = "framerate";
        public const string Query = "query";
        public const string PlaylistOrder = "playlist_order";
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
