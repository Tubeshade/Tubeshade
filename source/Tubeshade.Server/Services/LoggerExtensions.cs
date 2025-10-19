using Microsoft.Extensions.Logging;
using static Microsoft.Extensions.Logging.LogLevel;

namespace Tubeshade.Server.Services;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, Debug, "Created temporary directory {Path}")]
    internal static partial void Created(this ILogger logger, string path);

    [LoggerMessage(2, Debug, "Deleting temporary directory {Path}")]
    internal static partial void Deleting(this ILogger logger, string path);

    [LoggerMessage(3, Trace, """
                             ffmpeg output:
                             {Output}
                             {Error}
                             """)]
    internal static partial void FfmpegOutput(this ILogger logger, string? output, string? error);

    [LoggerMessage(4, Debug, "Copying existing audio stream")]
    internal static partial void CopyingAudio(this ILogger logger);

    [LoggerMessage(5, Debug, "Transcoding audio to {AudioCodec} at {AudioBitRate}")]
    internal static partial void TranscodingAudio(this ILogger logger, string audioCodec, string audioBitRate);
}
