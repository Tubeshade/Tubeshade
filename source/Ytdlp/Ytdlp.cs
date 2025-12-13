using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp.Options;
using Ytdlp.Processes;

namespace Ytdlp;

public sealed class Ytdlp
{
    private readonly string _ytdlpPath;
    private readonly string _ffmpegPath;

    public Ytdlp(string ytdlpPath, string ffmpegPath)
    {
        _ytdlpPath = ytdlpPath;
        _ffmpegPath = ffmpegPath;
    }

    public async Task<VideoData> GetVideoData(
        string url,
        OptionSet overrideOptions,
        CancellationToken cancellationToken)
    {
        var options = new OptionSet
        {
            IgnoreConfig = true,
            NoPlaylist = true,
            Downloader = "m3u8:native",
            DownloaderArgs = "ffmpeg:-nostats -loglevel 0",
            RestrictFilenames = false,
            ForceOverwrites = true,
            NoOverwrites = false,
            NoPart = true,
            FfmpegLocation = _ffmpegPath,
            Print = "after_move:outfile: %(filepath)s",
            DumpSingleJson = true,
            FlatPlaylist = true,
            WriteComments = false,
        };

        options.OverrideOptions(overrideOptions);

        var arguments = $"{options} -- \"{url}\"";

        using var process = new CancelableProcess(_ytdlpPath, arguments);
        var exitCode = await process.Run(cancellationToken);
        if (exitCode is not 0 || process.Output.ToList() is not [var json])
        {
            throw new NotImplementedException();
        }

        var data = JsonSerializer.Deserialize<VideoData>(json, YtdlpSerializerContext.Default.VideoData);
        return data ?? throw new NotImplementedException();
    }
}
