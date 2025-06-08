using System.IO;

namespace Tubeshade.Server.Configuration;

public sealed class YtdlpOptions
{
    public const string SectionName = "Ytdlp";

    public required string YtdlpPath { get; set; } = @"D:\_sort\_downloads\yt-dlp\yt-dlp.exe";

    public required string FfmpefgPath { get; set; } = @"D:\_sort\_downloads\yt-dlp\ffmpeg.exe";

    public required string TempPath { get; set; } = Path.GetTempPath();
}
