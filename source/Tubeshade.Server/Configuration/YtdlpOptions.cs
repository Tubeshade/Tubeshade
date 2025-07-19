using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Tubeshade.Server.Configuration;

public sealed class YtdlpOptions
{
    public const string SectionName = "Ytdlp";

    [Required]
    public required string YtdlpPath { get; set; }

    [Required]
    public required string FfmpefgPath { get; set; }

    public required string TempPath { get; set; } = Path.GetTempPath();

    public string? CookiesFromBrowser { get; set; }
}
