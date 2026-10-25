using System;
using System.IO;
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
    private const string PrivateVideoWarning = "WARNING: [youtube] Private video";

    private static readonly string[] SelectedFormatPropertyNames =
    [
        "format_id",
        "format",
        "requested_formats",
        "requested_downloads",
    ];

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

    public Task<RunResult<VideoData>> FetchAsync(
        string url,
        OptionSet options,
        CancellationToken cancellationToken)
    {
        return FetchAsync(url, options, null, cancellationToken);
    }

    public async Task<RunResult<VideoData>> FetchAsync(
        string url,
        OptionSet options,
        string? infoJsonFilePath,
        CancellationToken cancellationToken)
    {
        using var process = new CancelableProcess(Path, options.ToArguments(url));

        JsonDocument? document = null;
        VideoData videoData;
        try
        {
            var exitCode = await process.Run(cancellationToken);
            if (process.OutputLines.ToArray() is not [var json] || exitCode is not 0)
            {
                return RunResult<VideoData>.Failed(process.ErrorLines.ToArray());
            }

            document = JsonDocument.Parse(json);
            videoData = document.Deserialize(YouTubeSerializerContext.Default.VideoData)
                        ?? throw new InvalidOperationException("Deserialized video data to null");
        }
        catch (Exception)
        {
            document?.Dispose();
            return RunResult<VideoData>.Failed(process.ErrorLines.ToArray());
        }

        using (document)
        {
            videoData.IsPrivate =
                process.ErrorLines.Any(line => line.StartsWith(PrivateVideoWarning, StringComparison.Ordinal));

            // Writing the file is not a yt-dlp failure, so exceptions are not converted to a failed result
            if (infoJsonFilePath is { } path)
            {
                await WriteInfoJson(document, path, cancellationToken);
            }
        }

        return RunResult<VideoData>.Successful(videoData, process.ErrorLines.ToArray());
    }

    private static async Task WriteInfoJson(JsonDocument document, string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(filePath);
        await using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            // When loading the info json, yt-dlp only overwrites these properties if a format is selected,
            // so they need to be removed to not report the format selected here when no formats match
            if (!SelectedFormatPropertyNames.Contains(property.Name))
            {
                property.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        await writer.FlushAsync(cancellationToken);
    }
}
