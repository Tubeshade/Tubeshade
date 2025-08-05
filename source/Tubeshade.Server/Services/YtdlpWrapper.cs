using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tubeshade.Data.Preferences;
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

    public async ValueTask<VideoData> FetchPlaylistEntryUrls(
        string playlistUrl,
        int? count,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var youtube = new YoutubeDL
        {
            YoutubeDLPath = options.YtdlpPath,
            FFmpegPath = options.FfmpegPath,
        };

        var fetchResult = await youtube.RunVideoDataFetch(
            playlistUrl,
            cancellationToken,
            false,
            false,
            new OptionSet
            {
                Cookies = cookieFilepath,
                CookiesFromBrowser = options.CookiesFromBrowser,
                PlaylistItems = count.HasValue ? $"1:{count}" : null,
                YesPlaylist = false,
                FlatPlaylist = true,
            });

        if (!fetchResult.Success)
        {
            throw new(string.Join(Environment.NewLine, fetchResult.ErrorOutput));
        }

        if (fetchResult.Data.Entries.Any(data => data.ResultType is not MetadataType.Url))
        {
            throw new NotSupportedException("Playlist entry is not a url, despite specifying flat playlist");
        }

        return fetchResult.Data;
    }

    public async ValueTask<RunResult<VideoData>> FetchVideoData(
        string videoUrl,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var youtube = new YoutubeDL
        {
            YoutubeDLPath = options.YtdlpPath,
            FFmpegPath = options.FfmpegPath,
        };

        return await youtube.RunVideoDataFetch(
            videoUrl,
            cancellationToken,
            true,
            false,
            new OptionSet
            {
                Cookies = cookieFilepath,
                CookiesFromBrowser = options.CookiesFromBrowser,
                PlaylistItems = "0",
            });
    }

    public async ValueTask<VideoData> FetchVideoFormatData(
        string videoUrl,
        string format,
        string? cookieFilepath,
        PlayerClient? client,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var youtube = new YoutubeDL
        {
            YoutubeDLPath = options.YtdlpPath,
            FFmpegPath = options.FfmpegPath,
        };
        var youtubeClient = client is not null
            ? new MultiValue<string>($"youtube:player_client={client.Name}")
            : null;

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
                ExtractorArgs = youtubeClient,
            });

        if (!result.Success)
        {
            throw new(string.Join(Environment.NewLine, result.ErrorOutput));
        }

        return result.Data;
    }

    public async ValueTask<(string SelectedFormat, FormatData[] Formats)[]> SelectFormats(
        string videoUrl,
        IEnumerable<string> formats,
        string? cookieFilepath,
        PlayerClient? client,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var youtube = new YoutubeDL
        {
            YoutubeDLPath = options.YtdlpPath,
            FFmpegPath = options.FfmpegPath,
        };
        var youtubeClient = client is not null
            ? new MultiValue<string>($"youtube:player_client={client.Name}")
            : null;

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
                    ExtractorArgs = youtubeClient,
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
            .Select(tuple =>
            {
                var formatIds = tuple.result.Data.FormatID.Split('+');
                var videoFormats = formatIds
                    .Select(formatId => tuple.result.Data.Formats.Single(format => format.FormatId == formatId))
                    .ToArray();

                return (tuple.format, videoFormats);
            })
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
