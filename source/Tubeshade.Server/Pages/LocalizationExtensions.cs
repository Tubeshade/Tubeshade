using Tubeshade.Data.Preferences;
using Tubeshade.Server.Resources;

namespace Tubeshade.Server.Pages;

internal static class LocalizationExtensions
{
    extension(DownloadVideos download)
    {
        internal string? LocalizedName => SharedResources
            .ResourceManager
            .GetString($"Preferences_{nameof(DownloadVideos)}_{download.Name}");
    }

    extension(DownloadMethod method)
    {
        internal string? LocalizedName => SharedResources
            .ResourceManager
            .GetString($"Preferences_{nameof(DownloadMethod)}_{method.Name}");
    }
}
