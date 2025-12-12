using Tubeshade.Data.Preferences;

namespace Tubeshade.Server.Services;

internal static class PreferencesExtensions
{
    internal static void ApplyDefaults(this PreferencesEntity preferences)
    {
        preferences.Formats ??= YoutubeService.DefaultVideoFormats;
        preferences.DownloadVideos ??= DownloadVideos.None;
        preferences.VideosCount ??= YoutubeService.DefaultVideoCount;
        preferences.LiveStreamsCount ??= 0;
        preferences.ShortsCount ??= 0;
        preferences.PlaybackSpeed ??= 1;
    }
}
