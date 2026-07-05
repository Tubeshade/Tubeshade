using System;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Media;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class SortVideoBy : SmartEnum<SortVideoBy>, IParsable<SortVideoBy>
{
    private const string DownloadedAtExpression =
        // lang=sql
        "(SELECT MAX(video_files.downloaded_at) FROM media.video_files WHERE video_files.video_id = videos.id)";

    private const string StorageSizeExpression =
        // lang=sql
        "(SELECT SUM(video_files.storage_size) FROM media.video_files WHERE video_files.video_id = videos.id)";

    private const string FramerateExpression =
        // lang=sql
        "(SELECT MAX(video_files.framerate) FROM media.video_files WHERE video_files.video_id = videos.id)";

    // Probably going to regret this at some point, but ORDER BY random() works for the current table sizes
    public static readonly SortVideoBy Random = new(Names.Random, "random()", 1);
    public static readonly SortVideoBy CreatedAt = new(Names.CreatedAt, 2);
    public static readonly SortVideoBy ModifiedAt = new(Names.ModifiedAt, 3);
    public static readonly SortVideoBy PublishedAt = new(Names.PublishedAt, 4);
    public static readonly SortVideoBy RefreshedAt = new(Names.RefreshedAt, 5);
    public static readonly SortVideoBy ViewCount = new(Names.ViewCount, 6);
    public static readonly SortVideoBy LikeCount = new(Names.LikeCount, 7);
    public static readonly SortVideoBy DownloadedAt = new(Names.DownloadedAt, DownloadedAtExpression, 8);
    public static readonly SortVideoBy Duration = new(Names.Duration, 9);
    public static readonly SortVideoBy StorageSize = new(Names.StorageSize, StorageSizeExpression, 10);
    public static readonly SortVideoBy Framerate = new(Names.Framerate, FramerateExpression, 11);

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
        public const string ViewCount = "view_count";
        public const string LikeCount = "like_count";
        public const string DownloadedAt = "downloaded_at";
        public const string Duration = "duration";
        public const string StorageSize = "storage_size";
        public const string Framerate = "framerate";
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
