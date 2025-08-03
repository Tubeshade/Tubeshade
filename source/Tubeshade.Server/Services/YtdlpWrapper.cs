using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tubeshade.Server.Configuration;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace Tubeshade.Server.Services;

public sealed class YtdlpWrapper
{
    private readonly ILogger<YtdlpWrapper> _logger;
    private readonly IOptionsMonitor<YtdlpOptions> _optionsMonitor;

    public YtdlpWrapper(ILogger<YtdlpWrapper> logger, IOptionsMonitor<YtdlpOptions> optionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    public async ValueTask<VideoData> FetchUnknownUrlData(
        string url,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting metadata for {Url}", url);

        var options = _optionsMonitor.CurrentValue;
        var youtube = new YoutubeDL
        {
            YoutubeDLPath = options.YtdlpPath,
            FFmpegPath = options.FfmpegPath,
        };

        var result = await youtube.RunVideoDataFetch(
            url,
            cancellationToken,
            true,
            false,
            new OptionSet
            {
                Cookies = cookieFilepath,
                CookiesFromBrowser = options.CookiesFromBrowser,
                PlaylistItems = "0",
            });

        if (!result.Success)
        {
            throw new Exception(string.Join(Environment.NewLine, result.ErrorOutput));
        }

        return result.Data;
    }

    public async ValueTask<VideoData> FetchVideoFormatData(
        string videoUrl,
        string format,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var youtube = new YoutubeDL
        {
            YoutubeDLPath = options.YtdlpPath,
            FFmpegPath = options.FfmpegPath,
        };

        var result = await youtube.RunVideoDataFetch(
            videoUrl,
            cancellationToken,
            true,
            false,
            new OptionSet
            {
                Cookies = cookieFilepath,
                CookiesFromBrowser = options.CookiesFromBrowser,
                Format = format,
                NoPart = true,
                EmbedChapters = true,
            });

        if (!result.Success)
        {
            throw new(string.Join(Environment.NewLine, result.ErrorOutput));
        }

        return result.Data;
    }

    public async ValueTask<KeyValuePair<string, VideoData>[]> SelectFormats(
        string videoUrl,
        IEnumerable<string> formats,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var youtube = new YoutubeDL
        {
            YoutubeDLPath = options.YtdlpPath,
            FFmpegPath = options.FfmpegPath,
        };

        var tasks = formats.Select(async format =>
        {
            var result = await youtube.RunVideoDataFetch(
                videoUrl,
                cancellationToken,
                true,
                false,
                new OptionSet
                {
                    Cookies = cookieFilepath,
                    CookiesFromBrowser = options.CookiesFromBrowser,
                    Format = format,
                    NoPart = true,
                    EmbedChapters = true,
                });

            return (result, format);
        });

        var results = await Task.WhenAll(tasks);
        if (results.Any(tuple => !tuple.result.Success))
        {
            var failedResults = results.Where(tuple => !tuple.result.Success).Select(tuple => tuple.result.ErrorOutput);
            throw new Exception(string.Join(Environment.NewLine, failedResults.SelectMany(lines => lines)));
        }

        if (results.Any(tuple => tuple.result.Data.ResultType is not MetadataType.Video))
        {
            throw new InvalidOperationException("Unexpected metadata type when downloading video");
        }

        return results
            .Select(tuple => new KeyValuePair<string, VideoData>(tuple.format, tuple.result.Data))
            .ToArray();
    }

    public async ValueTask DownloadThumbnail(
        string thumbnailUrl,
        string path,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var youtube = new YoutubeDL
        {
            YoutubeDLPath = options.YtdlpPath,
            FFmpegPath = options.FfmpegPath,
        };

        var result = await youtube.RunWithOptions(
            thumbnailUrl,
            new OptionSet
            {
                Output = "thumbnail.%(ext)s",
                Paths = path,
                Cookies = cookieFilepath,
                CookiesFromBrowser = options.CookiesFromBrowser,
            },
            cancellationToken);

        if (!result.Success)
        {
            throw new(string.Join(Environment.NewLine, result.ErrorOutput));
        }
    }
}
