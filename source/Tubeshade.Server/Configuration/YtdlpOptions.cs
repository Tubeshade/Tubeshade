using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Tubeshade.Server.Configuration;

public sealed class YtdlpOptions
{
    public const string SectionName = "Ytdlp";

    [Required]
    public required string YtdlpPath { get; set; }

    [Required]
    public required string FfmpegPath { get; set; }

    [Required]
    public required string FfprobePath { get; set; }

    [Required]
    public required string TempPath { get; set; } = Path.GetTempPath();

    public string? JavascriptRuntimePath { get; set; }

    public long? LimitRate { get; set; } = 10 * 1024 * 1024;

    public decimal? LimitMultiplier { get; set; }

    [AllowedValues(null, "brave", "chrome", "chromium", "edge", "firefox", "opera", "safari", "vivaldi", "whale")]
    public string? CookiesFromBrowser { get; set; }
}
