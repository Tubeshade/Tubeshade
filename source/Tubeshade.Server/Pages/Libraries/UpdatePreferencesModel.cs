using System.ComponentModel;
using Tubeshade.Data.Preferences;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class UpdatePreferencesModel
{
    public decimal? PlaybackSpeed { get; set; }

    public int? VideosCount { get; set; }

    public int? LiveStreamsCount { get; set; }

    public int? ShortsCount { get; set; }

    public string? PlayerClient { get; set; }

    public DownloadVideos? DownloadVideos { get; set; }

    public string? Formats { get; set; }

    [Browsable(false)]
    public PreferencesEntity? Effective { get; init; }

    public UpdatePreferencesModel()
    {
    }

    public UpdatePreferencesModel(PreferencesEntity? preferences)
    {
        PlaybackSpeed = preferences?.PlaybackSpeed;
        VideosCount = preferences?.VideosCount;
        LiveStreamsCount = preferences?.LiveStreamsCount;
        ShortsCount = preferences?.ShortsCount;
        PlayerClient = preferences?.PlayerClient?.Name;
        DownloadVideos = preferences?.DownloadVideos;
        Formats = preferences?.Formats is { Length: > 0 } formats ? string.Join(',', formats) : null;
    }

    public PreferencesEntity ToPreferences()
    {
        var client = !string.IsNullOrWhiteSpace(PlayerClient)
            ? Data.Preferences.PlayerClient.FromName(PlayerClient, true)
            : null;

        return new PreferencesEntity
        {
            PlaybackSpeed = PlaybackSpeed,
            VideosCount = VideosCount,
            LiveStreamsCount = LiveStreamsCount,
            ShortsCount = ShortsCount,
            SubscriptionScheduleId = null,
            PlayerClient = client,
            DownloadVideos = DownloadVideos,
            Formats = string.IsNullOrWhiteSpace(Formats) ? null : Formats.Split(','),
        };
    }

    public void UpdatePreferences(PreferencesEntity preferences)
    {
        var client = !string.IsNullOrWhiteSpace(PlayerClient)
            ? Data.Preferences.PlayerClient.FromName(PlayerClient, true)
            : null;

        preferences.PlaybackSpeed = PlaybackSpeed;
        preferences.VideosCount = VideosCount;
        preferences.LiveStreamsCount = LiveStreamsCount;
        preferences.ShortsCount = ShortsCount;
        preferences.PlayerClient = client;
        preferences.DownloadVideos = DownloadVideos;
        preferences.Formats = string.IsNullOrWhiteSpace(Formats) ? null : Formats.Split(',');
    }
}
