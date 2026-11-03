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
    private const int InfoJsonBufferSize = 64 * 1024;

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
        await using var process = new CancelableProcess(Path, options.ToArguments(url));

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
        await using var process = new CancelableProcess(Path, options.ToArguments(url), standardOutputEvents: false);

        var stdoutPath = infoJsonFilePath is null
            ? null
            : $"{infoJsonFilePath}.out";

        try
        {
            var runTask = process.Run(cancellationToken);
            VideoData? videoData = null;

            if (stdoutPath is null)
            {
                videoData = await DeserializeVideoData(process.Output, cancellationToken);

                // If the output is not valid, the rest of it still needs to be read for yt-dlp to exit
                await process.Output.CopyToAsync(Stream.Null, cancellationToken);
            }
            else
            {
                await using var outputFile = File.Create(stdoutPath);
                await process.Output.CopyToAsync(outputFile, cancellationToken);
            }

            if (await runTask is not 0)
            {
                return RunResult<VideoData>.Failed(process.ErrorLines.ToArray());
            }

            if (stdoutPath is not null)
            {
                await using var output = File.OpenRead(stdoutPath);
                videoData = await DeserializeVideoData(output, cancellationToken);
            }

            if (videoData is null)
            {
                return RunResult<VideoData>.Failed(process.ErrorLines.ToArray());
            }

            videoData.IsPrivate = process.ErrorLines.Any(line => line.StartsWith(PrivateVideoWarning, StringComparison.Ordinal));

            if (infoJsonFilePath is not null && stdoutPath is not null)
            {
                await WriteInfoJson(stdoutPath, infoJsonFilePath, cancellationToken);
            }

            return RunResult<VideoData>.Successful(videoData, process.ErrorLines.ToArray());
        }
        finally
        {
            if (stdoutPath is not null)
            {
                File.Delete(stdoutPath);
            }
        }
    }

    /// <returns>The deserialized video data, or <see langword="null"/> if the output is not valid video data.</returns>
    private static async Task<VideoData?> DeserializeVideoData(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync(stream, YouTubeSerializerContext.Default.VideoData, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task WriteInfoJson(string stdoutPath, string infoPath, CancellationToken cancellationToken)
    {
        await using var inputStream = File.OpenRead(stdoutPath);
        await using var outputStream = File.Create(infoPath);
        await using var writer = new Utf8JsonWriter(outputStream);

        // Copied token by token, since even a single property value, like formats, can be too large to buffer
        var buffer = new byte[InfoJsonBufferSize];
        var length = 0;
        var isFinalBlock = false;
        var state = new JsonReaderState();
        var skippedDepth = -1;

        while (!isFinalBlock)
        {
            var bytesRead = await inputStream.ReadAsync(buffer.AsMemory(length), cancellationToken);
            isFinalBlock = bytesRead is 0;
            length += bytesRead;

            var bytesConsumed = CopyInfoJsonTokens(buffer.AsSpan(0, length), isFinalBlock, ref state, ref skippedDepth, writer);
            length -= bytesConsumed;
            buffer.AsSpan(bytesConsumed, length).CopyTo(buffer);

            // a single token did not fit in the buffer
            if (length == buffer.Length)
            {
                Array.Resize(ref buffer, buffer.Length * 2);
            }

            if (writer.BytesPending >= InfoJsonBufferSize)
            {
                await writer.FlushAsync(cancellationToken);
            }
        }

        await writer.FlushAsync(cancellationToken);
    }

    /// <returns>The number of bytes consumed from <paramref name="json"/>.</returns>
    private static int CopyInfoJsonTokens(
        ReadOnlySpan<byte> json,
        bool isFinalBlock,
        ref JsonReaderState state,
        ref int skippedDepth,
        Utf8JsonWriter writer)
    {
        var reader = new Utf8JsonReader(json, isFinalBlock, state);

        while (reader.Read())
        {
            if (skippedDepth >= 0)
            {
                if (reader.CurrentDepth == skippedDepth &&
                    reader.TokenType is not (JsonTokenType.StartObject or JsonTokenType.StartArray))
                {
                    skippedDepth = -1;
                }

                continue;
            }

            switch (reader.TokenType)
            {
                // When loading the info json, yt-dlp only overwrites these properties if a format is selected,
                // so they need to be removed to not report the format selected here when no formats match
                case JsonTokenType.PropertyName when reader.CurrentDepth is 1 && IsSelectedFormatProperty(ref reader):
                    skippedDepth = reader.CurrentDepth;
                    break;

                case JsonTokenType.PropertyName when reader.ValueIsEscaped:
                    writer.WritePropertyName(reader.GetString()!);
                    break;

                case JsonTokenType.PropertyName:
                    writer.WritePropertyName(reader.ValueSpan);
                    break;

                case JsonTokenType.String when reader.ValueIsEscaped:
                    writer.WriteStringValue(reader.GetString());
                    break;

                case JsonTokenType.String:
                    writer.WriteStringValue(reader.ValueSpan);
                    break;

                case JsonTokenType.Number:
                    writer.WriteRawValue(reader.ValueSpan, skipInputValidation: true);
                    break;

                case JsonTokenType.True or JsonTokenType.False:
                    writer.WriteBooleanValue(reader.TokenType is JsonTokenType.True);
                    break;

                case JsonTokenType.Null:
                    writer.WriteNullValue();
                    break;

                case JsonTokenType.StartObject:
                    writer.WriteStartObject();
                    break;

                case JsonTokenType.EndObject:
                    writer.WriteEndObject();
                    break;

                case JsonTokenType.StartArray:
                    writer.WriteStartArray();
                    break;

                case JsonTokenType.EndArray:
                    writer.WriteEndArray();
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected token {reader.TokenType} in yt-dlp output");
            }
        }

        state = reader.CurrentState;
        return (int)reader.BytesConsumed;
    }

    private static bool IsSelectedFormatProperty(ref Utf8JsonReader reader)
    {
        foreach (var propertyName in SelectedFormatPropertyNames)
        {
            if (reader.ValueTextEquals(propertyName))
            {
                return true;
            }
        }

        return false;
    }
}
