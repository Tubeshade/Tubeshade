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

/// <inheritdoc />
public sealed class YtdlpWrapper : IYtdlpWrapper
{
    private readonly ILogger<YtdlpWrapper> _logger;
    private readonly IOptionsMonitor<YtdlpOptions> _optionsMonitor;

    public YtdlpWrapper(ILogger<YtdlpWrapper> logger, IOptionsMonitor<YtdlpOptions> optionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    /// <inheritdoc />
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

        var customOptions = new List<IOption>();
        if (options.JavascriptRuntimePath is { } javascriptRuntimePath)
        {
            customOptions.Add(new Option<string>("--js-runtimes") { Value = javascriptRuntimePath });
        }

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
                IgnoreErrors = true,
                IgnoreNoFormatsError = true,
                CustomOptions = customOptions.ToArray(),
            });

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
        var options = _optionsMonitor.CurrentValue;
        var youtube = new YoutubeDL
        {
            YoutubeDLPath = options.YtdlpPath,
            FFmpegPath = options.FfmpegPath,
        };

        var customOptions = new List<IOption>();
        if (options.JavascriptRuntimePath is { } javascriptRuntimePath)
        {
            customOptions.Add(new Option<string>("--js-runtimes") { Value = javascriptRuntimePath });
        }

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
                CustomOptions = customOptions.ToArray(),
            });

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
        var options = _optionsMonitor.CurrentValue;
        var youtube = new YoutubeDL
        {
            YoutubeDLPath = options.YtdlpPath,
            FFmpegPath = options.FfmpegPath,
        };

        var customOptions = new List<IOption>();
        if (options.JavascriptRuntimePath is { } javascriptRuntimePath)
        {
            customOptions.Add(new Option<string>("--js-runtimes") { Value = javascriptRuntimePath });
        }

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
                IgnoreNoFormatsError = true,
                CustomOptions = customOptions.ToArray(),
            });
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
        var options = _optionsMonitor.CurrentValue;
        var youtube = new YoutubeDL
        {
            YoutubeDLPath = options.YtdlpPath,
            FFmpegPath = options.FfmpegPath,
        };

        var youtubeClient = client is not null
            ? new MultiValue<string>($"youtube:player_client={client.Name}")
            : null;

        var customOptions = new List<IOption>();
        if (options.JavascriptRuntimePath is { } javascriptRuntimePath)
        {
            customOptions.Add(new Option<string>("--js-runtimes") { Value = javascriptRuntimePath });
        }

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
                IgnoreNoFormatsError = ignoreNoFormatsError,
                CustomOptions = customOptions.ToArray(),
            });

        if (!result.Success)
        {
            throw new(string.Join(Environment.NewLine, result.ErrorOutput));
        }

        return result.Data;
    }

    /// <inheritdoc />
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

        var customOptions = new List<IOption>();
        if (options.JavascriptRuntimePath is { } javascriptRuntimePath)
        {
            customOptions.Add(new Option<string>("--js-runtimes") { Value = javascriptRuntimePath });
        }

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
                    CustomOptions = customOptions.ToArray(),
                });

            return (result, format);
        });

        var results = await Task.WhenAll(tasks);
        if (results.Any(tuple => !tuple.result.Success))
        {
            var failedResults = results.Where(tuple => !tuple.result.Success).Select(tuple => tuple.result.ErrorOutput);
            throw new Exception(string.Join(Environment.NewLine, failedResults.SelectMany(lines => lines)));
        }

        if (results.Any(tuple => tuple.result.Data!.ResultType is not MetadataType.Video))
        {
            throw new InvalidOperationException("Unexpected metadata type when downloading video");
        }

        return results
            .Select(tuple =>
            {
                var formatIds = tuple.result.Data!.FormatID!.Split('+');
                var videoFormats = formatIds
                    .Select(formatId => tuple.result.Data.Formats!.Single(format => format.FormatId == formatId))
                    .ToArray();

                return (tuple.format, videoFormats);
            })
            .ToArray();
    }

    /// <inheritdoc />
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

        var customOptions = new List<IOption>();
        if (options.JavascriptRuntimePath is { } javascriptRuntimePath)
        {
            customOptions.Add(new Option<string>("--js-runtimes") { Value = javascriptRuntimePath });
        }

        var result = await youtube.RunWithOptions(
            thumbnailUrl,
            new OptionSet
            {
                Output = "thumbnail.%(ext)s",
                Paths = path,
                Cookies = cookieFilepath,
                CookiesFromBrowser = options.CookiesFromBrowser,
                CustomOptions = customOptions.ToArray(),
            },
            cancellationToken);

        if (!result.Success)
        {
            throw new(string.Join(Environment.NewLine, result.ErrorOutput));
        }
    }
}
