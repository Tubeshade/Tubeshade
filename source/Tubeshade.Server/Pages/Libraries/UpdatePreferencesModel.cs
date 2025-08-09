namespace Tubeshade.Server.Pages.Libraries;

public sealed class UpdatePreferencesModel
{
    public decimal? PlaybackSpeed { get; set; }

    public int? VideosCount { get; set; }

    public int? LiveStreamsCount { get; set; }

    public int? ShortsCount { get; set; }

    public string? PlayerClient { get; set; }
}
