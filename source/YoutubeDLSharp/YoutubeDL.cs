using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp.Helpers;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace YoutubeDLSharp;

/// <summary>
/// A class providing methods for downloading videos using yt-dlp.
/// </summary>
public sealed class YoutubeDL
{
    /// <summary>
    /// Path to the yt-dlp executable.
    /// </summary>
    public required string YoutubeDLPath { get; init; }

    /// <summary>
    /// Path to the FFmpeg executable.
    /// </summary>
    public required string FFmpegPath { get; init; }

    /// <summary>
    /// Path of the folder where items will be downloaded to.
    /// </summary>
    public string OutputFolder { get; set; } = Environment.CurrentDirectory;

    /// <summary>
    /// Template of the name of the downloaded file on yt-dlp style.
    /// See https://github.com/yt-dlp/yt-dlp#output-template.
    /// </summary>
    public string OutputFileTemplate { get; set; } = "%(title)s [%(id)s].%(ext)s";

    /// <summary>
    /// Runs yt-dlp with the given option set and additional parameters.
    /// </summary>
    /// <param name="url">The video URL passed to yt-dlp.</param>
    /// <param name="options">The OptionSet of yt-dlp options.</param>
    /// <param name="cancellationToken">A CancellationToken used to cancel the process.</param>
    /// <returns>A RunResult object containing the path to the downloaded and converted video.</returns>
    public async Task<RunResult<string>> RunWithOptions(
        string url,
        OptionSet options,
        CancellationToken cancellationToken)
    {
        var process = CreateYoutubeDlProcess();
        var (code, errors) = await ProcessRunner.RunThrottled(process, [url], options, cancellationToken);
        return new RunResult<string>(code == 0, errors, string.Empty);
    }

    /// <summary>
    /// Runs a fetch of information for the given video without downloading the video.
    /// </summary>
    /// <param name="url">The URL of the video to fetch information for.</param>
    /// <param name="ct">A CancellationToken used to cancel the process.</param>
    /// <param name="flat">If set to true, does not extract information for each video in a playlist.</param>
    /// <param name="fetchComments">If set to true, fetch comment data for the given video.</param>
    /// <param name="overrideOptions">Override options of the default option set for this run.</param>
    /// <returns>A RunResult object containing a VideoData object with the requested video information.</returns>
    public async Task<RunResult<VideoData>> RunVideoDataFetch(
        string url,
        CancellationToken ct = default,
        bool flat = true,
        bool fetchComments = false,
        OptionSet? overrideOptions = null)
    {
        var opts = GetDownloadOptions();
        opts.DumpSingleJson = true;
        opts.FlatPlaylist = flat;
        opts.WriteComments = fetchComments;
        if (overrideOptions != null)
        {
            opts = opts.OverrideOptions(overrideOptions);
        }

        VideoData? videoData = null;
        var process = CreateYoutubeDlProcess();
        process.OutputReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                return;
            }

            videoData = JsonSerializer.Deserialize(args.Data, YouTubeSerializerContext.Default.VideoData)
                        ?? throw new JsonException("Deserialized yt-dlp data to null");
        };
        var (code, errors) = await ProcessRunner.RunThrottled(process, [url], opts, ct);
        return new RunResult<VideoData>(code == 0 && videoData is not null, errors, videoData!);
    }

    /// <summary>
    /// Runs a download of the specified video with an optional conversion afterwards.
    /// </summary>
    /// <param name="url">The URL of the video to be downloaded.</param>
    /// <param name="format">A format selection string in yt-dlp style.</param>
    /// <param name="mergeFormat">If a merge is required, the container format of the merged downloads.</param>
    /// <param name="recodeFormat">The video format the output will be recoded to after download.</param>
    /// <param name="overrideOptions">Override options of the default option set for this run.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A RunResult object containing the path to the downloaded and converted video.</returns>
    public async Task<RunResult<string>> RunVideoDownload(
        string url,
        string format = "bestvideo+bestaudio/best",
        DownloadMergeFormat mergeFormat = DownloadMergeFormat.Unspecified,
        VideoRecodeFormat recodeFormat = VideoRecodeFormat.None,
        OptionSet? overrideOptions = null, CancellationToken cancellationToken = default)
    {
        var opts = GetDownloadOptions();
        opts.Format = format;
        opts.MergeOutputFormat = mergeFormat;
        opts.RecodeVideo = recodeFormat;
        if (overrideOptions != null)
        {
            opts = opts.OverrideOptions(overrideOptions);
        }

        var outputFile = string.Empty;
        var process = CreateYoutubeDlProcess();
        var (code, errors) = await ProcessRunner.RunThrottled(process, [url], opts, cancellationToken);
        return new RunResult<string>(code == 0, errors, outputFile);
    }

    /// <summary>
    /// Returns an option set with default options used for most downloading operations.
    /// </summary>
    private OptionSet GetDownloadOptions() => new()
    {
        IgnoreErrors = true,
        IgnoreConfig = true,
        NoPlaylist = true,
        Downloader = "m3u8:native",
        DownloaderArgs = "ffmpeg:-nostats -loglevel 0",
        Output = Path.Combine(OutputFolder, OutputFileTemplate),
        RestrictFilenames = false,
        ForceOverwrites = true,
        NoOverwrites = false,
        NoPart = true,
        FfmpegLocation = Utils.GetFullPath(FFmpegPath),
        Print = "after_move:outfile: %(filepath)s"
    };

    private YoutubeDLProcess CreateYoutubeDlProcess() => new(YoutubeDLPath);
}
