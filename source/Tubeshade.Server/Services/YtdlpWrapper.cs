using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Server.Configuration;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace Tubeshade.Server.Services;

/// <inheritdoc />
public sealed class YtdlpWrapper : IYtdlpWrapper
{
    private readonly ILogger<YtdlpWrapper> _logger;
    private readonly IOptionsMonitor<YtdlpOptions> _optionsMonitor;
    private readonly Ytdlp.Ytdlp _ytdlp;

    public YtdlpWrapper(ILogger<YtdlpWrapper> logger, IOptionsMonitor<YtdlpOptions> optionsMonitor, Ytdlp.Ytdlp ytdlp)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _ytdlp = ytdlp;
    }

    /// <inheritdoc />
    public async ValueTask<VideoData> FetchUnknownUrlData(
        string url,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        _logger.UnknownUrlMetadata(url);

        var optionSet = GetDefaultOptions(cookieFilepath);
        optionSet.PlaylistItems = "0";
        optionSet.IgnoreNoFormatsError = true;
        optionSet.DumpSingleJson = true;

        var result = await _ytdlp.FetchAsync(url, optionSet, cancellationToken);
        if (result.ErrorOutput is { Length: > 0 } errorOutput)
        {
            _logger.StandardError(_ytdlp.Path, string.Join(Environment.NewLine, errorOutput.Select(line => line ?? string.Empty)));
        }

        if (!result.Success)
        {
            throw new Exception(string.Join(Environment.NewLine, result.ErrorOutput));
        }

        return result.Data;
    }

    /// <inheritdoc />
    public async ValueTask<VideoData> FetchPlaylistEntryUrls(
        string playlistUrl,
        int? count,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        var optionSet = GetDefaultOptions(cookieFilepath);
        optionSet.PlaylistItems = count.HasValue ? $"1:{count}" : null;
        optionSet.YesPlaylist = false;
        optionSet.DumpSingleJson = true;

        var fetchResult = await _ytdlp.FetchAsync(playlistUrl, optionSet, cancellationToken);
        if (fetchResult.ErrorOutput is { Length: > 0 } errorOutput)
        {
            _logger.StandardError(
                _ytdlp.Path,
                string.Join(Environment.NewLine, errorOutput.Select(line => line ?? string.Empty)));
        }

        if (!fetchResult.Success || fetchResult.Data?.Entries is null)
        {
            throw new(string.Join(Environment.NewLine, fetchResult.ErrorOutput));
        }

        if (fetchResult.Data.Entries.Any(data => data.ResultType is not MetadataType.Url))
        {
            throw new NotSupportedException("Playlist entry is not a url, despite specifying flat playlist");
        }

        return fetchResult.Data;
    }

    /// <inheritdoc />
    public async ValueTask<RunResult<VideoData>> FetchVideoData(
        string videoUrl,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        var optionSet = GetDefaultOptions(cookieFilepath);
        optionSet.PlaylistItems = "0";
        optionSet.IgnoreNoFormatsError = true;
        optionSet.DumpSingleJson = true;

        var result = await _ytdlp.FetchAsync(videoUrl, optionSet, cancellationToken);

        if (result.ErrorOutput is { Length: > 0 } errorOutput)
        {
            _logger.StandardError(
                _ytdlp.Path,
                string.Join(Environment.NewLine, errorOutput.Select(line => line ?? string.Empty)));
        }

        return result;
    }

    /// <inheritdoc />
    public async ValueTask<VideoData> FetchVideoFormatData(
        string videoUrl,
        string format,
        string? cookieFilepath,
        PlayerClient? client,
        bool ignoreNoFormatsError,
        CancellationToken cancellationToken)
    {
        var optionSet = GetDefaultOptions(cookieFilepath);
        optionSet.Format = format;
        optionSet.EmbedChapters = true;
        optionSet.IgnoreNoFormatsError = ignoreNoFormatsError;
        optionSet.DumpSingleJson = true;
        optionSet.ExtractorArgs = client is not null
            ? new MultiValue<string>($"youtube:player_client={client.Name}")
            : null;

        var result = await _ytdlp.FetchAsync(videoUrl, optionSet, cancellationToken);

        if (result.ErrorOutput is { Length: > 0 } errorOutput)
        {
            _logger.StandardError(
                _ytdlp.Path,
                string.Join(Environment.NewLine, errorOutput.Select(line => line ?? string.Empty)));
        }

        if (!result.Success)
        {
            throw new(string.Join(Environment.NewLine, result.ErrorOutput));
        }

        return result.Data;
    }

    /// <inheritdoc />
    public async ValueTask<FormatData[][]> SelectFormats(
        string videoUrl,
        IEnumerable<string> formats,
        string? cookieFilepath,
        PlayerClient? client,
        CancellationToken cancellationToken)
    {
        var youtubeClient = client is not null
            ? new MultiValue<string>($"youtube:player_client={client.Name}")
            : null;

        var tasks = formats.Select(async format =>
        {
            var optionSet = GetDefaultOptions(cookieFilepath);
            optionSet.EmbedChapters = true;
            optionSet.Format = format;
            optionSet.ExtractorArgs = youtubeClient;
            optionSet.DumpSingleJson = true;

            var result = await _ytdlp.FetchAsync(videoUrl, optionSet, cancellationToken);
            if (result.ErrorOutput is { Length: > 0 } errorOutput)
            {
                _logger.StandardError(
                    _ytdlp.Path,
                    string.Join(Environment.NewLine, errorOutput.Select(line => line ?? string.Empty)));
            }

            return result;
        });

        var results = await Task.WhenAll(tasks);
        if (results.Any(tuple => !tuple.Success))
        {
            var failedResults = results.Where(tuple => !tuple.Success).Select(tuple => tuple.ErrorOutput);
            throw new Exception(string.Join(Environment.NewLine, failedResults.SelectMany(lines => lines)));
        }

        if (results.Any(tuple => tuple.Data!.ResultType is not MetadataType.Video))
        {
            throw new InvalidOperationException("Unexpected metadata type when downloading video");
        }

        return results
            .Select(result =>
            {
                var formatIds = result.Data!.FormatId!.Split('+');
                var videoFormats = formatIds
                    .Select(formatId => result.Data.Formats!.Single(format => format.FormatId == formatId))
                    .ToArray();

                return videoFormats;
            })
            .ToArray();
    }

    /// <inheritdoc />
    public async ValueTask DownloadThumbnail(
        string url,
        string path,
        string fileNameWithoutExtension,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        var optionSet = GetDefaultOptions(cookieFilepath);
        optionSet.Output = $"{fileNameWithoutExtension}.%(ext)s";
        optionSet.Paths = path;
        optionSet.WriteThumbnail = true;
        optionSet.SkipDownload = true;
        optionSet.IgnoreNoFormatsError = true;

        var result = await _ytdlp.RunAsync(url, optionSet, cancellationToken);

        if (result.ErrorOutput is { Length: > 0 } errorOutput)
        {
            _logger.StandardError(
                _ytdlp.Path,
                string.Join(Environment.NewLine, errorOutput.Select(line => line ?? string.Empty)));
        }

        if (!result.Success)
        {
            throw new(string.Join(Environment.NewLine, result.ErrorOutput));
        }
    }

    /// <inheritdoc />
    public async ValueTask DownloadChannelThumbnails(
        string channelUrl,
        string path,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        var optionSet = GetDefaultOptions(cookieFilepath);
        optionSet.Output = "thumbnail:thumbnail.%(ext)s";
        optionSet.Paths = path;
        optionSet.SkipDownload = true;
        optionSet.WriteAllThumbnails = true;
        optionSet.PlaylistItems = "0";

        var result = await _ytdlp.RunAsync(channelUrl, optionSet, cancellationToken);

        if (result.ErrorOutput is { Length: > 0 } errorOutput)
        {
            _logger.StandardError(
                _ytdlp.Path,
                string.Join(Environment.NewLine, errorOutput.Select(line => line ?? string.Empty)));
        }

        if (!result.Success)
        {
            throw new(string.Join(Environment.NewLine, result.ErrorOutput));
        }
    }

    /// <inheritdoc />
    public async Task<RunResult<string>> DownloadVideo(
        string videoUrl,
        string format,
        VideoContainerType containerType,
        string outputFolder,
        string outputTemplate,
        string? cookieFilepath,
        long? limitRate,
        PlayerClient? client,
        CancellationToken cancellationToken)
    {
        var mergeFormat = containerType.Name switch
        {
            VideoContainerType.Names.Mp4 => DownloadMergeFormat.Mp4,
            VideoContainerType.Names.WebM => DownloadMergeFormat.Webm,
            _ => DownloadMergeFormat.Unspecified,
        };

        var subtitleOption = new Option<string>("-o")
        {
            Value = $"subtitle:{Path.Combine(outputFolder, "subtitles.%(ext)s")}"
        };

        var youtubeClient = client is not null
            ? new MultiValue<string>($"youtube:player_client={client.Name}")
            : null;

        var optionSet = GetDefaultOptions(cookieFilepath, subtitleOption);
        optionSet.Format = format;
        optionSet.Output = Path.Combine(outputFolder, outputTemplate);
        optionSet.LimitRate = limitRate;
        optionSet.WriteSubs = true;
        optionSet.NoWriteAutoSubs = true;
        optionSet.SubFormat = "vtt";
        optionSet.SubLangs = "all,-live_chat";
        optionSet.EmbedChapters = true;
        optionSet.ExtractorArgs = youtubeClient;
        optionSet.MergeOutputFormat = mergeFormat;
        optionSet.RecodeVideo = VideoRecodeFormat.None;

        var result = await _ytdlp.RunAsync(videoUrl, optionSet, cancellationToken);

        if (result.ErrorOutput is { Length: > 0 } errorOutput)
        {
            _logger.StandardError(
                _ytdlp.Path,
                string.Join(Environment.NewLine, errorOutput.Select(line => line ?? string.Empty)));
        }

        return result;
    }

    /// <inheritdoc />
    public OptionSet GetDownloadFormatArgs(string format, string output, string? cookieFilepath, long? limitRate,
        PlayerClient? client)
    {
        // todo: subtitles
        var youtubeClient = client is not null
            ? new MultiValue<string>($"youtube:player_client={client.Name}")
            : null;

        var optionSet = GetDefaultOptions(cookieFilepath);
        optionSet.Format = format;
        optionSet.Output = output;
        optionSet.LimitRate = limitRate;
        optionSet.WriteSubs = true;
        optionSet.NoWriteAutoSubs = true;
        optionSet.SubFormat = "vtt";
        optionSet.SubLangs = "all,-live_chat";
        optionSet.EmbedChapters = true;
        optionSet.ExtractorArgs = youtubeClient;
        optionSet.RecodeVideo = VideoRecodeFormat.None;

        return optionSet;
    }

    private OptionSet GetDefaultOptions(string? cookieFilepath, params ReadOnlySpan<IOption> additionalOptions)
    {
        var options = _optionsMonitor.CurrentValue;

        var customOptions = new List<IOption>();
        if (options.JavascriptRuntimePath is { } javascriptRuntimePath)
        {
            customOptions.Add(new Option<string>("--js-runtimes") { Value = javascriptRuntimePath });
        }

        customOptions.AddRange(additionalOptions);

        return new OptionSet
        {
            IgnoreErrors = true,
            IgnoreConfig = true,
            NoPlaylist = true,
            FlatPlaylist = true,
            Downloader = "m3u8:native",
            DownloaderArgs = "ffmpeg:-nostats -loglevel 0",
            RestrictFilenames = false,
            ForceOverwrites = true,
            NoOverwrites = false,
            NoPart = true,
            FfmpegLocation = options.FfmpegPath,
            Verbose = true,
            CustomOptions = customOptions.ToArray(),
            Cookies = cookieFilepath,
            CookiesFromBrowser = options.CookiesFromBrowser,
            WriteComments = false,
        };
    }
}
