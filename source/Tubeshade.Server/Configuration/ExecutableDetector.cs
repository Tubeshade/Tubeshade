using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tubeshade.Server.Configuration;

public sealed class ExecutableDetector : IConfigureOptions<YtdlpOptions>
{
    private readonly ILogger<ExecutableDetector> _logger;

    public ExecutableDetector(ILogger<ExecutableDetector> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void Configure(YtdlpOptions options)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _logger.LogDebug("Executable auto-detection is only supported for linux");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.FfmpefgPath) || !File.Exists(options.FfmpefgPath))
        {
            _logger.LogInformation("ffmpeg path is not set, trying to find it");
            if (LocateExecutable("ffmpeg") is { } path)
            {
                _logger.LogInformation("Found ffmpeg at {FfmpegPath}", path);
                options.FfmpefgPath = path;
            }
        }

        if (string.IsNullOrWhiteSpace(options.YtdlpPath) || !File.Exists(options.YtdlpPath))
        {
            _logger.LogInformation("yt-dlp path is not set, trying to find it");
            if (LocateExecutable("yt-dlp") is { } path)
            {
                _logger.LogInformation("Found yt-dlp at {YtdlpPath}", path);
                options.YtdlpPath = path;
            }
        }
    }

    private string? LocateExecutable(string name)
    {
        var processInfo = new ProcessStartInfo("which", [name])
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        var process = Process.Start(processInfo);
        if (process is null)
        {
            _logger.LogWarning("Failed start process for locating executable");
            return null;
        }

        var output = process.StandardOutput.ReadToEnd();
        _logger.LogDebug("Received output {ProcessOutput}", output);

        process.WaitForExit();
        if (process.ExitCode is not 0 || string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        return output.Trim();
    }
}
