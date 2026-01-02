using System.Resources;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Resources;

namespace Tubeshade.Server.Pages;

internal static class LocalizationExtensions
{
    private static readonly ResourceManager Manager = SharedResources.ResourceManager;

    extension(ExternalAvailability availability)
    {
        internal string? LocalizedName => Manager
            .GetString($"Video_{nameof(ExternalAvailability)}_{availability.Name}");
    }

    extension(DownloadVideos download)
    {
        internal string? LocalizedName => Manager
            .GetString($"Preferences_{nameof(DownloadVideos)}_{download.Name}");
    }

    extension(DownloadMethod method)
    {
        internal string? LocalizedName => Manager
            .GetString($"Preferences_{nameof(DownloadMethod)}_{method.Name}");
    }

    extension(TaskSource source)
    {
        internal string? LocalizedName => Manager
            .GetString($"Tasks_{nameof(TaskSource)}_{source.Name}");
    }

    extension(VideoType type)
    {
        internal string? LocalizedName => Manager
            .GetString($"Filters_{nameof(VideoType)}_{type.Name}");
    }
}
