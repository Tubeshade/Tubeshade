namespace Tubeshade.Data.Media.Videos;

public sealed class VideoPlaybackStatistics
{
    public required int PlayCount { get; set; }

    public required double WatchTime { get; set; }

    public required double PlaybackTime { get; set; }
}
