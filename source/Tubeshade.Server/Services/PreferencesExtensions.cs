using Tubeshade.Data.Preferences;

namespace Tubeshade.Server.Services;

internal static class PreferencesExtensions
{
    internal static void ApplyDefaults(this PreferencesEntity preferences)
    {
        preferences.Formats ??= YoutubeIndexingService.DefaultVideoFormats;
        preferences.DownloadVideos ??= DownloadVideos.None;
        preferences.DownloadMethod ??= DownloadMethod.Default;
        preferences.VideosCount ??= YoutubeIndexingService.DefaultVideoCount;
        preferences.LiveStreamsCount ??= 0;
        preferences.ShortsCount ??= 0;
        preferences.PlaybackSpeed ??= 1;
    }
}
