using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;
using Ytdlp.Processes;

namespace Ytdlp;

public sealed class Ytdlp
{
    public Ytdlp(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public async Task<RunResult<string>> RunAsync(string url, OptionSet options, CancellationToken cancellationToken)
    {
        using var process = new CancelableProcess(Path, options.ToArguments(url));

        try
        {
            var exitCode = await process.Run(cancellationToken);
            return exitCode is 0
                ? RunResult<string>.Successful(string.Empty, process.ErrorLines.ToArray())
                : RunResult<string>.Failed(process.ErrorLines.ToArray());
        }
        catch (Exception)
        {
            return RunResult<string>.Failed(process.ErrorLines.ToArray());
        }
    }

    public async Task<RunResult<VideoData>> FetchAsync(
        string url,
        OptionSet options,
        CancellationToken cancellationToken)
    {
        using var process = new CancelableProcess(Path, options.ToArguments(url));

        try
        {
            var exitCode = await process.Run(cancellationToken);
            if (process.OutputLines.ToArray() is not [var json] || exitCode is not 0)
            {
                return RunResult<VideoData>.Failed(process.ErrorLines.ToArray());
            }

            var videoData = JsonSerializer.Deserialize(json, YouTubeSerializerContext.Default.VideoData)
                            ?? throw new InvalidOperationException("Deserialized video data to null");

            return RunResult<VideoData>.Successful(videoData, process.ErrorLines.ToArray());
        }
        catch (Exception)
        {
            return RunResult<VideoData>.Failed(process.ErrorLines.ToArray());
        }
    }
}
