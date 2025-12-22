using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration;
using Ytdlp.Processes;

namespace Tubeshade.Server.Services.Ffmpeg;

public sealed class FfmpegService
{
    private readonly ILogger<FfmpegService> _logger;
    private readonly IOptionsMonitor<YtdlpOptions> _options;

    public FfmpegService(ILogger<FfmpegService> logger, IOptionsMonitor<YtdlpOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public async ValueTask<ProbeResponse> AnalyzeFile(
        string filePath,
        CancellationToken cancellationToken)
    {
        var args = new[]
        {
            "-v",
            "quiet",
            "-i",
            filePath,
            "-print_format",
            "json",
            "-show_format",
            "-show_streams",
        };

        var processInfo = new ProcessStartInfo(_options.CurrentValue.FfprobePath, args)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
        };

        using var process = Process.Start(processInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start ffprobe");
        }

        ProbeResponse response;
        await using (cancellationToken.Register(state => (state as Process)?.Kill(), process))
        {
            string error;
            using (var reader = new StreamReader(process.StandardError.BaseStream))
            {
                error = await reader.ReadToEndAsync(cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new(error);
            }

            string output;
            using (var reader = new StreamReader(process.StandardOutput.BaseStream))
            {
                output = await reader.ReadToEndAsync(cancellationToken);
            }

            response = JsonSerializer.Deserialize(output, FfmpegContext.Default.ProbeResponse)!;
            await process.WaitForExitAsync(cancellationToken);
        }

        return response;
    }

    public async ValueTask<string> RemuxFile(
        string filePath,
        VideoContainerType type,
        ProbeResponse response,
        CancellationToken cancellationToken)
    {
        var outputFilePath = Path.ChangeExtension(filePath, type.Name);
        if (response.Streams is not [var first, var second])
        {
            throw new("Expected video to have 2 streams");
        }

        var (_, audio) = (first, second) switch
        {
            _ when first.CodecType is "video" && second.CodecType is "audio" => (first, second),
            _ when first.CodecType is "audio" && second.CodecType is "video" => (second, first),
            _ => throw new("Expected video to have a single video and single audio stream")
        };

        var args = new List<string> { "-i", filePath, "-vcodec", "copy" };

        if (type == VideoContainerType.WebM && audio.CodecName is "opus" or "vorbis")
        {
            _logger.CopyingAudio();
            args.AddRange(["-acodec", "copy"]);
        }
        else
        {
            var (audioCodec, audioBitRate) = type.Name switch
            {
                VideoContainerType.Names.Mp4 => ("aac", "256k"),
                VideoContainerType.Names.WebM => ("libopus", "256k"),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unexpected container type"),
            };

            _logger.TranscodingAudio(audioCodec, audioBitRate);
            args.AddRange(["-acodec", audioCodec, "-b:a", audioBitRate]);
        }

        args.Add(outputFilePath);

        var processInfo = new ProcessStartInfo(_options.CurrentValue.FfmpegPath, args)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
        };

        using var process = Process.Start(processInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start ffmpeg");
        }

        string? error;
        string? output;
        await using (cancellationToken.Register(state => (state as Process)?.Kill(), process))
        {
            using (var reader = new StreamReader(process.StandardError.BaseStream))
            {
                error = await reader.ReadToEndAsync(cancellationToken);
            }

            using (var reader = new StreamReader(process.StandardOutput.BaseStream))
            {
                output = await reader.ReadToEndAsync(cancellationToken);
            }

            await process.WaitForExitAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.StandardError(processInfo.FileName, error);
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            _logger.StandardOutput(processInfo.FileName, output);
        }

        if (process.ExitCode is not 0 && !string.IsNullOrWhiteSpace(error))
        {
            throw new(error);
        }

        File.Delete(filePath);
        return outputFilePath;
    }

    public async Task CombineStreams(
        string videoFilePath,
        string audioFilePath,
        string outputFilePath,
        CancellationToken cancellationToken)
    {
        var args = new List<string>
        {
            "-nostdin",
            "-y", // Overwrite output files without asking.
            "-rw_timeout", // Maximum time to wait for (network) read/write operations to complete, in microseconds.
            "5M",
            "-follow", // If set to 1, the protocol will retry reading at the end of the file, allowing reading files that still are being written
            "1",
            "-i", // input file url
            $"file:{videoFilePath}",
            "-rw_timeout", // Maximum time to wait for (network) read/write operations to complete, in microseconds.
            "5M",
            "-follow", // If set to 1, the protocol will retry reading at the end of the file, allowing reading files that still are being written
            "1",
            "-i", // input file url
            $"file:{audioFilePath}",
            "-map",
            "0:v:0",
            "-map",
            "1:a:0",
            "-c",
            "copy",
        };

        if (outputFilePath.EndsWith("mp4"))
        {
            args.AddRange(
            [
                "-movflags",
                "+faststart+frag_keyframe+separate_moof+default_base_moof+delay_moov+empty_moov",
                "-frag_duration",
                "15M",
            ]);
        }

        args.Add(outputFilePath);

        using var ffmpegProcess = new CancelableProcess(_options.CurrentValue.FfmpegPath, string.Join(' ', args));
        ffmpegProcess.OutputReceived += OnOutputReceived;
        ffmpegProcess.ErrorReceived += OnErrorReceived;

        var exitCode = await ffmpegProcess.Run(cancellationToken);

        ffmpegProcess.OutputReceived -= OnOutputReceived;
        ffmpegProcess.ErrorReceived -= OnErrorReceived;

        if (exitCode is not 0)
        {
            throw new("Failed to combine video and audio streams");
        }
    }

    /// <remarks>Defragments MP4 videos.</remarks>
    public async Task Copy(
        string inputFilePath,
        string outputFilePath,
        CancellationToken cancellationToken)
    {
        var args = new List<string> { "-nostdin", "-y", "-i", $"file:{inputFilePath}", "-c", "copy", outputFilePath };

        using var ffmpegProcess = new CancelableProcess(_options.CurrentValue.FfmpegPath, string.Join(' ', args));
        ffmpegProcess.OutputReceived += OnOutputReceived;
        ffmpegProcess.ErrorReceived += OnErrorReceived;

        var exitCode = await ffmpegProcess.Run(cancellationToken);

        ffmpegProcess.OutputReceived -= OnOutputReceived;
        ffmpegProcess.ErrorReceived -= OnErrorReceived;

        if (exitCode is not 0)
        {
            throw new("Failed to copy streams");
        }
    }

    private void OnErrorReceived(CancelableProcess process, ReceivedLineEventArgs eventArgs)
    {
        _logger.StandardError(process.FileName, eventArgs.Line);
    }

    private void OnOutputReceived(CancelableProcess process, ReceivedLineEventArgs eventArgs)
    {
        _logger.StandardOutput(process.FileName, eventArgs.Line);
    }
}
