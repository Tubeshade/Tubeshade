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
        var args = string.Join(' ', options.GetOptionFlags().Append(url));
        using var process = new CancelableProcess(Path, args);

        try
        {
            var exitCode = await process.Run(cancellationToken);
            return exitCode is 0
                ? RunResult<string>.Successful(string.Empty, process.Error.ToArray())
                : RunResult<string>.Failed(process.Error.ToArray());
        }
        catch (Exception)
        {
            return RunResult<string>.Failed(process.Error.ToArray());
        }
    }

    public async Task<RunResult<VideoData>> FetchAsync(
        string url,
        OptionSet options,
        CancellationToken cancellationToken)
    {
        var args = string.Join(' ', options.GetOptionFlags().Append(url));
        using var process = new CancelableProcess(Path, args);

        try
        {
            var exitCode = await process.Run(cancellationToken);
            if (process.Output.ToArray() is not [var json] || exitCode is not 0)
            {
                return RunResult<VideoData>.Failed(process.Error.ToArray());
            }

            var videoData = JsonSerializer.Deserialize(json, YouTubeSerializerContext.Default.VideoData)
                            ?? throw new InvalidOperationException("Deserialized video data to null");

            return RunResult<VideoData>.Successful(videoData, process.Error.ToArray());
        }
        catch (Exception)
        {
            return RunResult<VideoData>.Failed(process.Error.ToArray());
        }
    }
}
