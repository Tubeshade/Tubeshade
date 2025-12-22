using System;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Preferences;

public sealed record PreferencesEntity : ModifiableEntity
{
    public decimal? PlaybackSpeed { get; set; }

    public int? VideosCount { get; set; }

    public int? LiveStreamsCount { get; set; }

    public int? ShortsCount { get; set; }

    public Guid? SubscriptionScheduleId { get; set; }

    public PlayerClient? PlayerClient { get; set; }

    public DownloadVideos? DownloadVideos { get; set; }

    public DownloadMethod? DownloadMethod { get; set; }

    public string[]? Formats { get; set; }
}
