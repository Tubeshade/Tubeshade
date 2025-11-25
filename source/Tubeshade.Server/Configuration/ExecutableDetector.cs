using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tubeshade.Server.Configuration;

public sealed class ExecutableDetector : IPostConfigureOptions<YtdlpOptions>, IValidateOptions<YtdlpOptions>
{
    private readonly ILogger<ExecutableDetector> _logger;

    public ExecutableDetector(ILogger<ExecutableDetector> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, YtdlpOptions options)
    {
        var errors = new List<string>(3);

        if (!File.Exists(options.FfmpegPath))
        {
            errors.Add($"ffmpeg does not exist at path '{options.FfmpegPath}'");
        }

        if (!File.Exists(options.FfprobePath))
        {
            errors.Add($"ffprobe does not exist at path '{options.FfprobePath}'");
        }

        if (!File.Exists(options.YtdlpPath))
        {
            errors.Add($"yt-dlp does not exist at path '{options.YtdlpPath}'");
        }

        return errors is []
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    /// <inheritdoc />
    public void PostConfigure(string? name, YtdlpOptions options)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _logger.LogDebug("Executable auto-detection is only supported for linux");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.FfmpegPath))
        {
            _logger.LogInformation("ffmpeg path is not set, trying to find it");
            if (LocateExecutable("ffmpeg") is { } path)
            {
                _logger.LogInformation("Found ffmpeg at {FfmpegPath}", path);
                options.FfmpegPath = path;
            }
        }

        if (string.IsNullOrWhiteSpace(options.FfprobePath))
        {
            _logger.LogInformation("ffprobe path is not set, trying to find it");
            if (LocateExecutable("ffprobe") is { } path)
            {
                _logger.LogInformation("Found ffprobe at {FfprobePath}", path);
                options.FfprobePath = path;
            }
        }

        if (string.IsNullOrWhiteSpace(options.YtdlpPath))
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

        using var process = Process.Start(processInfo);
        if (process is null)
        {
            _logger.LogWarning("Failed start process for locating executable");
            return null;
        }

        var output = process.StandardOutput.ReadToEnd().Trim();
        _logger.LogDebug("Received output {ProcessOutput}", output);

        process.WaitForExit();
        if (process.ExitCode is not 0 || string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        return output;
    }
}
