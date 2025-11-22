using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Tasks;

public sealed class TaskType : SmartEnum<TaskType>
{
    public static readonly TaskType Index = new(Names.Index, 1);
    public static readonly TaskType DownloadVideo = new(Names.DownloadVideo, 2);
    public static readonly TaskType ScanChannel = new(Names.ScanChannel, 3);
    public static readonly TaskType ScanSubscriptions = new(Names.ScanSubscriptions, 4);
    public static readonly TaskType ScanSponsorBlockSegments = new(Names.ScanSponsorBlockSegments, 5);
    public static readonly TaskType RefreshSubscriptions = new(Names.RefreshSubscriptions, 6);
    public static readonly TaskType ReindexVideos = new(Names.ReindexVideos, 7);

    private TaskType(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Index = "index";
        public const string DownloadVideo = "download_video";
        public const string ScanChannel = "scan_channel";
        public const string ScanSubscriptions = "scan_subscriptions";
        public const string ScanSponsorBlockSegments = "scan_sponsor_block_segments";
        public const string RefreshSubscriptions = "refresh_subscriptions";
        public const string ReindexVideos = "reindex_videos";
    }
}
